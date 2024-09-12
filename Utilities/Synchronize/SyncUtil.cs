using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Synchronize;
using log4net;
using NHibernate.Exceptions;
using System.Threading.Tasks;
using RadialReview.Utilities.NHibernate;
using static RadialReview.Middleware.Request.HttpContextExtensions.HttpContextItems;
using System.Diagnostics;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities;
using RadialReview.Core.Accessors.StrictlyAfterExecutors;

namespace RadialReview.Utilities.Synchronize
{
  public class SyncUtil
  {

    public static ulong RecentHistoryId = 0;
    public static CircularQueue<SyncMetaData> RecentHistory = new CircularQueue<SyncMetaData>(1000);

    protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public static TimeSpan Buffer = TimeSpan.FromSeconds(40);
    public class SyncTiny
    {
      public long? ClientTimestamp { get; set; }

      public DateTime DbTimestamp { get; set; }

      public long Id { get; set; }
    }

    public static HttpContextItemKey NO_SYNC_EXCEPTION = new HttpContextItemKey("noSyncException");

    public static string UserActionKey(UserOrganizationModel caller, SyncAction action)
    {
      return (caller.GetClientRequestId()) + "_" + action.ToString();
    }

    public static async Task<bool> EnsureStrictlyAfter(UserOrganizationModel caller, SyncAction action, IStrictlyAfter functions, bool noSyncException = false)
    {
      Func<ISession, PermissionsUtility, Task> afterUpdateFunction = null;
      var permissionFunction = new Func<ISession, PermissionsUtility, Task>(async (s, perms) => await functions.EnsurePermitted(s, perms));
      if (functions.Behavior.HasAfterUpdateFunction)
      {
        afterUpdateFunction = new Func<ISession, PermissionsUtility, Task>(async (s, perms) => await functions.AfterAtomicUpdate(s, perms));
      }
      return await EnsureStrictlyAfter(caller, action, permissionFunction, (s) => functions.AtomicUpdate(s), afterUpdateFunction, noSyncException);
    }


    public static async Task<bool> EnsureStrictlyAfter(UserOrganizationModel caller, SyncAction action, Func<ISession, PermissionsUtility, Task> permissionsPrefunction, Func<IOrderedSession, Task> atomic, Func<ISession, PermissionsUtility, Task> afterAtomicFunction, bool noSyncException = false)
    {
      return await EnsureStrictlyAfter(caller, x => action, permissionsPrefunction, atomic, afterAtomicFunction, noSyncException);
    }

    public static async Task<bool> EnsureStrictlyAfter(UserOrganizationModel caller, Func<IStatelessSession, SyncAction> actionSelector, Func<ISession, PermissionsUtility, Task> permissionsPrefunction, Func<IOrderedSession, Task> atomic, Func<ISession, PermissionsUtility, Task> afterAtomicFunction, bool noSyncException = false, TestHooks hooks = null)
    {

      if (permissionsPrefunction != null)
      {
        using (var s = HibernateSession.GetCurrentSession())
        {
          using (var tx = s.BeginTransaction())
          {
            var perms = PermissionsUtility.Create(s, caller);
            await permissionsPrefunction(s, perms);
            if (Config.IsLocal() && s.IsDirty())
            {
              throw new Exception("Should not perform updates in the permissionsPrefunction");
            }
            tx.Commit();
            s.Flush();
          }
        }
      }


      var shouldThrowSyncException = !noSyncException;
      try
      {
        if (shouldThrowSyncException && HttpContextHelper.Current != null && HttpContextHelper.Current.Items != null && HttpContextHelper.Current.GetRequestItemOrDefault(NO_SYNC_EXCEPTION, false))
        {
          noSyncException = true;
        }
      }
      catch (Exception e)
      {
      }

      //Required again after all the short circuits
      shouldThrowSyncException = !noSyncException;
      var clientUpdateTime = caller._ClientTimestamp;
      var hasError = false;
      var hasWarning = false;
      if (clientUpdateTime == null && shouldThrowSyncException)
      {
        hasWarning = true;
      }

      var allowedToUpdate = new Func<SyncLock, bool>(x => x != null && (x.LastClientUpdateTimeMs == null || clientUpdateTime == null || clientUpdateTime.Value - x.LastClientUpdateTimeMs.Value > 0));
      var wasUpdated = false;
      if (hooks != null)
      {
        hooks?.BeforePreCheck();
      }

      if (hooks != null)
      {
        hooks?.AfterPreCheck();
      }

      //Ensure only one...
      if (!hasError)
      {
        await SyncUtil.Lock(ss => UserActionKey(caller, actionSelector(ss)), clientUpdateTime, async (s, lck) => {
          var canUpdate = lck.LastClientUpdateTimeMs == null;
          canUpdate = canUpdate || clientUpdateTime == null || clientUpdateTime.Value - lck.LastClientUpdateTimeMs.Value > 0;
          canUpdate = canUpdate || lck.LastUpdateDb.Add(TimeSpan.FromMinutes(1)) < DateTime.UtcNow;
          if (allowedToUpdate(lck))
          {
            var os = OrderedSession.From(s, lck);
            await atomic(os);
            wasUpdated = true;
          }
          else
          {
            hasError = true;
          }
        });
        if (wasUpdated && afterAtomicFunction != null)
        {
          using (var s = HibernateSession.GetCurrentSession())
          {
            using (var tx = s.BeginTransaction())
            {
              var perms = PermissionsUtility.Create(s, caller);
              await afterAtomicFunction(s, perms);
              tx.Commit();
              s.Flush();
            }
          }
        }
      }

      if (shouldThrowSyncException && hasError)
        throw new SyncException("Out of sync", clientUpdateTime);
      if (Config.IsLocal() && hasWarning)
        throw new SyncException("Client timestamp was null. Make sure timestamp is sent or you'll have issues.", clientUpdateTime);
      return !hasError && !hasWarning;
    }

    public class TestHooks
    {
      public Action AfterLock { get; set; }

      public Action AfterUnlock { get; set; }

      public Action BeforeLock { get; set; }

      public Action BeforeUnlock { get; set; }

      public Action BeforePreCheck { get; set; }

      public Action AfterPreCheck { get; set; }
    }

    private static object TEST_LOCK = new object();
    private static object TEST_UNLOCK = new object();
    /// <summary>
    /// 
    /// </summary>
    /// <param name = "key"></param>
    /// <param name = "clientUpdateTimeMs"></param>
    /// <param name = "atomic">Your atomic action. tx.Commit() and s.Flush() are called automatically</param>
    /// <param name = "testHooks"></param>
    /// <returns></returns>
    public static async Task Lock(string key, long? clientUpdateTimeMs, Func<ISession, SyncLock, Task> atomic, TestHooks testHooks = null)
    {
      await Lock(x => key, clientUpdateTimeMs, atomic, testHooks);
    }

    public static async Task Lock(Func<IStatelessSession, string> keySelector, long? clientUpdateTimeMs, Func<ISession, SyncLock, Task> atomic, TestHooks testHooks = null)
    {
      var nil = await Lock(keySelector, clientUpdateTimeMs, async (s, lck) => {
        await atomic(s, lck);
        return false;
      }, testHooks);
    }

    public static async Task<T> Lock<T>(Func<IStatelessSession, string> keySelector, long? clientUpdateTimeMs, Func<ISession, SyncLock, Task<T>> atomic, TestHooks testHooks = null)
    {
      if (clientUpdateTimeMs == null)
      {
        //probably want to make sure its not null...
        int a = 0;
      }

      RecentHistoryId++;
      var metaData = new SyncMetaData()
      {
        Id = RecentHistoryId,
        Key = "-unset-",
        StartTime = DateTime.UtcNow
      };
      RecentHistory.Enqueue(metaData);

      var sw = Stopwatch.StartNew();

      var delay = 150; //ms
      var retry = (10000.0 / delay); //10 seconds of retrys before failing
                                     //Make sure lock key exists
      var key = "";
      while (true)
      {
        retry -= 1;
        try
        {
          using (var s = HibernateSession.GetDatabaseSessionFactory().OpenStatelessSession())
          {
            using (var tx = s.BeginTransaction())
            {
              //The below if block might always return false
              if (s is SingleRequestSession)
              {
                var srs = (SingleRequestSession)s;
                if (srs.GetCurrentContext().Depth != 0)
                  throw new Exception("Lock must be called outside of a session.");
              }

              key = keySelector(s);
              metaData.Key = key;
              if (retry < 0)
              {
                //Failing after certain number of trys to avoid the server deadlocking.
                log.Error("Object was locked. Failed to update. [" + key + "]");
                throw new Exception("Object was locked. Failed to update. [" + key + "]");
              }

              var found = s.Get<SyncLock>(key, LockMode.Upgrade);
              if (found == null)
              {
                metaData.CreateAttempts++;
                var createKey = SyncLock.CREATE_KEY(key);
                metaData.CreateKey = createKey;
                //Didnt exists. Lets atomically create it
                //LockMode.Upgrade prevents creating simultaniously 
                //Use one of the N sync locks available to lock for the creation. You should use N keys so the service doesn't lock up.
                var createLock = s.Get<SyncLock>(createKey, LockMode.Upgrade);
                if (createLock == null)
                  throw new Exception("CreateLock '" + createKey + "' doesnt exist. Call ApplicationAccessor.EnsureExists()");
                //was it created in another thread while we were locked?
                if (s.Get<SyncLock>(key, LockMode.Upgrade) != null)
                {
                  //was already created in another thread....
                }
                else
                {
                  //doesn't exist. Lets create it..
                  s.Insert(new SyncLock() { Id = key, });
                }

                //s.Flush();
                tx.Commit();
              }
            }
          }

          break;
        }
        catch (GenericADOException e)
        {
          log.Info("Deadlock: " + key + " [retrying]");
          await Task.Delay(delay);
        }
        catch (StaleObjectStateException e)
        {
          log.Info("Deadlock(stale): " + key + " [retrying]");
          await Task.Delay(delay);
        }
        catch (Exception e)
        {
          throw;
        }
      }

      T result = default(T);
      //Lets lock on the thing we just created..
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          //var key = keySelector(s);
          //LOCK
          SyncLock lck;
          metaData.UpdateAttempts++;
          if (testHooks != null && testHooks.AfterLock != null)
          {
            //If we are testing, we need BeforeLock and Get to be atomic
            lock (TEST_LOCK)
            {
              testHooks.BeforeLock();
              lck = s.Get<SyncLock>(key, LockMode.Upgrade);
              testHooks.AfterLock();
              //Allowed to lock here since Get<> is atomic
            }
          }
          else
          {
            lck = s.Get<SyncLock>(key, LockMode.Upgrade); //Actual db lock happens on this line.
          }

          var now = DateTime.UtcNow;
          //atomic action here
          metaData.AtomicActionStarted = true;
          var atomicSW = Stopwatch.StartNew();
          result = await atomic(s, lck);
          metaData.AtomicActionEllapsedMs = atomicSW.ElapsedMilliseconds;
          metaData.AtomicActionComplete = true;
          lck.LastClientUpdateTimeMs = clientUpdateTimeMs ?? lck.LastClientUpdateTimeMs;
          lck.UpdateCount += 1;

          metaData.TotalUpdates = lck.UpdateCount;
          metaData.LastClientUpdateTimeMs = lck.LastClientUpdateTimeMs;

          s.Update(lck);
          if (testHooks != null && testHooks.BeforeUnlock != null)
          {
            //If we are testing, we need Commit and AfterLock to be atomic
            lock (TEST_UNLOCK)
            {
              testHooks.BeforeUnlock();
              tx.Commit();
              testHooks.AfterUnlock();
              //Allowed to lock here since Commit is atomic
            }
          }
          else
          {
            tx.Commit(); //Actual db unlock happens on this line.
          }

          s.Flush();
          //unlock
        }
      }

      metaData.Complete = true;
      metaData.TotalEllapsedMs = sw.ElapsedMilliseconds;

      return result;
    }

    public static async Task ExecuteNonAtomically(ISession s, PermissionsUtility perms, IStrictlyAfter executor)
    {
      await executor.EnsurePermitted(s, perms);
      await executor.AtomicUpdate(OrderedSession.Indifferent(s));
      if (executor.Behavior.HasAfterUpdateFunction)
      {
        await executor.AfterAtomicUpdate(s, perms);
      }
    }

    [Obsolete("Doesnt work", true)]
    private static bool IsStrictlyAfter(ISession s, string actionStr, long clientTimestamp, long callerId, Sync newSync, DateTime newSyncDbTimestamp, TimeSpan buffer)
    {
      var after = newSyncDbTimestamp;
      if ((newSyncDbTimestamp - DateTime.MinValue) > buffer)
        after = newSyncDbTimestamp.Subtract(buffer);
      s.Flush();
      var syncsUnfiltered = s.QueryOver<Sync>().Where(x => x.DeleteTime == null && x.DbTimestamp >= after && x.Action == actionStr && x.UserId == callerId).Select(x => x.Timestamp, x => x.DbTimestamp, x => x.Id).List<object[]>().Select(x => new SyncTiny { Id = (long)x[2], ClientTimestamp = (long)x[0], DbTimestamp = (DateTime)x[1] });
      var syncs = syncsUnfiltered.Where(x => x.Id != newSync.Id).ToList();
      if (!IsStrictlyAfter(clientTimestamp, syncs))
      {
        return false;
      }

      return true;
    }

    [Obsolete("Doesnt work", true)]
    private static bool IsStrictlyAfter(long clientTimestamp, List<SyncTiny> existingSyncs)
    {
      return existingSyncs.All(x => x.ClientTimestamp - clientTimestamp <= 0);
    }
  }
}
