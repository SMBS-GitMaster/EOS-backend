﻿using NHibernate;
using RadialReview.Controllers;
using RadialReview.Core.Accessors.StrictlyAfterExecutors;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.Rocks;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
//using System.Web.WebPages.Html;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using L10Controller = RadialReview.Core.Controllers.L10Controller;

namespace RadialReview.Accessors {

  public enum AttachRockType {
    Create,
    Existing,
    Unarchive,
  }

  public partial class L10Accessor : BaseAccessor {

    #region Goals
    public static List<RockModel> GetAllMyL10Rocks(UserOrganizationModel caller, long userId, long? periodId = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perm = PermissionsUtility.Create(s, caller);
          perm.ViewUserOrganization(userId, true);
          var myRocks = s.QueryOver<RockModel>()
            .Where(x => x.AccountableUser.Id == userId && x.DeleteTime == null)
            .List().ToList();

          L10Recurrence recurAlias = null;
          var nameIds = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
            .JoinAlias(x => x.L10Recurrence, () => recurAlias)
            .Where(x => x.DeleteTime == null && recurAlias.DeleteTime == null)
            .WhereRestrictionOn(x => x.ForRock.Id).IsIn(myRocks.Select(x => x.Id).ToList())
            .Select(x => x.ForRock.Id, x => recurAlias.Id, x => recurAlias.Name)
            .List<object[]>()
            .Select(x => new {
              RockId = (long)x[0],
              RecurrenceId = (long)x[1],
              RecurrenceName = (string)x[2]
            }).GroupBy(x => x.RockId)
            .ToDefaultDictionary(
              x => x.Key,
              x => x.Select(y => new NameId(y.RecurrenceName, y.RecurrenceId)).ToList(),
              x => new List<NameId>()
            );

          foreach (var r in myRocks) {
            r._Origins = nameIds[r.Id];
          }

          return myRocks;

        }
      }
    }

    private static void PopulateRockOrigins(ISession s, PermissionsUtility p, RockModel rock, bool includeDeletedMeetings) {
      p.ViewRock(rock.Id, includeDeletedMeetings);
      L10Recurrence recurAlias = null;
      var keys = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
        .JoinAlias(x => x.L10Recurrence, () => recurAlias)
        .Where(x => x.ForRock.Id == rock.Id && x.DeleteTime == null)
        .Select(x => x.L10Recurrence.Id, x => recurAlias.Name)
        .List<object[]>()
        .Select(x => new NameId((string)x[1], (long)x[0]))
        .ToList();

      foreach (var k in keys) {
        if (!p.IsPermitted(pp => p.ViewL10Recurrence(k.Id)))
          k.Name = "Unknown name";
      }
      rock._Origins = keys;

    }


    public static List<L10Recurrence.L10Recurrence_Rocks> GetRocksForRecurrence(UserOrganizationModel caller, long recurrenceId, bool includeArchives = false) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetRocksForRecurrence(s, perms, recurrenceId, includeArchives);
        }
      }
    }
    public static List<L10Recurrence.L10Recurrence_Rocks> GetRocksForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, bool includeArchives = false) {
      perms.ViewL10Recurrence(recurrenceId);
      RockModel rock = null;
      var q = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
        .JoinAlias(x => x.ForRock, () => rock).Where(x => x.L10Recurrence.Id == recurrenceId);
      if (!includeArchives) {
        q = q.Where(x => x.DeleteTime == null && rock.DeleteTime == null);
      }
      var found = q.Fetch(x => x.ForRock).Eager.List().ToList();
      foreach (var f in found) {
        if (f.ForRock.AccountableUser != null) {
          var a = f.ForRock.AccountableUser.GetName();
          var b = f.ForRock.AccountableUser.ImageUrl(true, ImageSize._32);
        }
      }
      return found;
    }

    public class MeetingRockAndMilestones {
      public L10Meeting.L10Meeting_Rock Rock { get; set; }
      public List<Milestone> Milestones { get; set; }

      public MeetingRockAndMilestones() {
        Milestones = new List<Milestone>();
      }
    }
    public static List<MeetingRockAndMilestones> GetRocksForMeeting(ISession s, PermissionsUtility perms, long recurrenceId, long meetingId) {
      perms.ViewL10Recurrence(recurrenceId).ViewL10Meeting(meetingId);
      var meetingRocks = s.QueryOver<L10Meeting.L10Meeting_Rock>()
        .Where(x => x.DeleteTime == null && x.ForRecurrence.Id == recurrenceId && x.L10Meeting.Id == meetingId)
        .Fetch(x => x.ForRock).Eager
        .List().ToList();

      var rockIds = meetingRocks.Select(x => x.ForRock.Id).ToList();

      //var rocks = s.QueryOver<RockModel>().WhereRestrictionOn(x => x.Id).IsIn(rockIds).Future();
      var milestones = s.QueryOver<Milestone>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.RockId).IsIn(rockIds).Future();


      var found = new List<MeetingRockAndMilestones>();

      foreach (var f in meetingRocks) {
        var toAdd = new MeetingRockAndMilestones();

        if (f.ForRock.AccountableUser == null) {
          f.ForRock.AccountableUser = s.Load<UserOrganizationModel>(f.ForRock.ForUserId);
        }

        var a = f.ForRock.AccountableUser.NotNull(x => x.GetName());
        var b = f.ForRock.AccountableUser.NotNull(x => x.ImageUrl(true, ImageSize._32));
        toAdd.Rock = f;
        toAdd.Milestones = milestones.Where(x => x.RockId == f.ForRock.Id).ToList();
        found.Add(toAdd);
      }
      return found;
    }

    public static List<MeetingRockAndMilestones> GetRocksForMeeting(UserOrganizationModel caller, long recurrenceId, long meetingId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetRocksForMeeting(s, perms, recurrenceId, meetingId);
        }
      }
    }

    public static async Task AttachRock(UserOrganizationModel caller, long recurrenceId, long rockId, bool vtoRock, AttachRockType attachType) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          await AttachRock(s, perms, recurrenceId, rockId, vtoRock, attachType);
          tx.Commit();
          s.Flush();
        }
      }
    }




    public static async Task AttachRock(ISession s, PermissionsUtility perms, long recurrenceId, long rockId, bool vtoRock, AttachRockType attachType) {
      var rock = s.Get<RockModel>(rockId);
      if (attachType == AttachRockType.Create) {
        perms.ViewUserOrganization(rock.ForUserId, false);
        //var any = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        //      .Where(x => x.L10Recurrence.Id == recurrenceId && x.User.Id == rock.ForUserId && x.DeleteTime == null)
        //      .RowCount();
        //if (any == 0) {
        //  throw new PermissionsException("Owner is not attached to this meeting");
        //}
      } else {
        perms.Or(x => x.ViewRock(rockId, attachType == AttachRockType.Unarchive), x => x.CanViewUserRocks(rock.ForUserId));
      }

      await _AddExistingRockToL10(s, perms, recurrenceId, rock, DateTime.UtcNow, vtoRock);
    }
    public static async Task<RockModel> CreateAndAttachRock(UserOrganizationModel caller, long recurrenceId, long owner, string message, bool vtoRock = false) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perm = PermissionsUtility.Create(s, caller);
          RockModel rock = await CreateAndAttachRock(s, perm, recurrenceId, owner, message, vtoRock);
          tx.Commit();
          s.Flush();
          return rock;
        }
      }
    }
    public static async Task<RockModel> CreateAndAttachRock(ISession s, PermissionsUtility perm, long recurrenceId, long owner, string message, bool vtoRock) {

      perm.CreateRocksForUser(owner, recurrenceId);
      perm.AdminL10Recurrence(recurrenceId);

      var rock = await RockAccessor.CreateRock(s, perm, owner, message, permittedForRecurrenceId: recurrenceId);
      await AttachRock(s, perm, recurrenceId, rock.Id, vtoRock, AttachRockType.Create);
      return rock;
    }
    [Obsolete("Avoid using")]
    public static async Task<RockModel> CreateOrAttachRock(UserOrganizationModel caller, long recurrenceId, L10Controller.AddRockVm model) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var rock = await CreateOrAttachRock(s, perms, recurrenceId, model);
          tx.Commit();
          s.Flush();
          return rock;
        }
      }
    }
    [Obsolete("Avoid using")]
    public static async Task<RockModel> CreateOrAttachRock(ISession s, PermissionsUtility perm, long recurrenceId, L10Controller.AddRockVm model) {
      var recur = s.Get<L10Recurrence>(recurrenceId);
      var attachType = AttachRockType.Existing;
      RockModel rock;
      if (model.SelectedRock == -3) {
        //Create new
        if (model.Rocks == null || !model.Rocks.Any()) {
          throw new PermissionsException("You must include a goal to create.");
        }

        rock = model.Rocks.SingleOrDefault();
        rock = await RockAccessor.CreateRock(s, perm, rock.ForUserId, rock.Rock);
        attachType = AttachRockType.Create;
      } else {
        //Find Existing
        rock = s.Get<RockModel>(model.SelectedRock);
        if (rock == null) {
          throw new PermissionsException("Goal does not exist.");
        }

        perm.ViewRock(rock.Id);
      }

      await AttachRock(s, perm, recurrenceId, rock.Id, false, attachType);
      return rock;
    }

    [Untested("EnsureStrictlyAfter")]
    public static async Task UpdateRock(UserOrganizationModel caller, long rockId, String rockMessage,
      RockState? state, long? ownerId, string connectionId, /* bool? delete = null,bool? companyRock = null,*/
      DateTime? dueDate = null, long? recurrenceRockId = null, bool? vtoRock = null, string noteId = null) {


      var now = DateTime.UtcNow;
      var updateRockExecutor = new UpdateRockExecutor(rockId, rockMessage, ownerId, state, dueDate, now, noteId);
      var setVtoExecutor = new SetVtoRockExecutor(recurrenceRockId??-1, vtoRock ?? false);

      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateRockCompletion(rockId), async (s, perms) => {
        //Permissions checks
        await updateRockExecutor.EnsurePermitted(s, perms);
        if (vtoRock != null && recurrenceRockId != null) {
          var recurRock = s.Get<L10Recurrence.L10Recurrence_Rocks>(recurrenceRockId.Value);
          if (recurRock.ForRock.Id != rockId) {
            throw new PermissionsException("Incorrect goal");
          }
          await setVtoExecutor.EnsurePermitted(s, perms);
        }
      }, async s => {
        await using (var rt = RealTimeUtility.Create(connectionId)) {
          //Sync Execution

          var perms = PermissionsUtility.Create(s, caller);
          var rock = s.Get<RockModel>(rockId);

          //var statusOnly = (dueDate == null || rock.DueDate == dueDate) &&
          //        (rockMessage == null || rock.Name == rockMessage) &&
          //        (ownerId == null || rock.AccountableUser.Id == ownerId) &&
          //        (vtoRock == null);

          //perms.EditRock(rock.Id, statusOnly);


          await updateRockExecutor.AtomicUpdate(s);//await RockAccessor.UpdateRock(s, perms, rockId, message: rockMessage, ownerId: ownerId, completion: state, dueDate: dueDate, now: now);
          

          if (vtoRock != null && recurrenceRockId != null) { //Hey.. I can't do anything without the RecurrenceRockId
            //var recurRock = s.Get<L10Recurrence.L10Recurrence_Rocks>(recurrenceRockId.Value);
            //if (recurRock.ForRock.Id != rockId) {
            //  throw new PermissionsException("Incorrect goal");
            //}
            await setVtoExecutor.AtomicUpdate(s); //await L10Accessor.SetVtoRock(s, perms, recurRock.Id, vtoRock.Value);
          }
        }
      }, async (s, perms) => {
        await _UpdateMeetingRockCompletionTimes_Unsafe(s, rockId, state, now);

        await updateRockExecutor.AfterAtomicUpdate(s, perms);
        if (vtoRock != null && recurrenceRockId != null)
        {
          await setVtoExecutor.AfterAtomicUpdate(s, perms);
        }

        //if (shouldExecute) {
        //  perms.EditRock(rockId, !anyNonStausUpdates); /*Permissions are also with the Update method. This call cannot be relied on however.*/
        //  s.Update(rock);
        //  var cc = perms.GetCaller();
        //  await HooksRegistry.Each<IRockHook>((ss, x) => x.UpdateRock(ss, cc, rock, updates));
        //}
      });

    }

    public static async Task RemoveRock(UserOrganizationModel caller, long recurrenceId, long rockId, DateTime detachTime, bool archiveWhenNotInMeetings = true) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await using (var rt = RealTimeUtility.Create()) {
            var perms = PermissionsUtility.Create(s, caller);

            await RemoveRock(s, perms, rt, recurrenceId, rockId, detachTime, archiveWhenNotInMeetings, deleteTime: detachTime);

            tx.Commit();
            s.Flush();
          }
        }
      }
    }


    //[Untested("Vto_Rocks",/* "Is the goal correctly removed in real-time from L10",/* "Is the goal correctly removed in real-time from VTO",*/ "Is goal correctly archived when existing in no meetings?")]
    public static async Task RemoveRock(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceId, long rockId, DateTime detachTime, bool archiveWhenNotInMeetings = true, DateTime? deleteTime = null, bool? archived = false) {
      perm.EditL10Recurrence(recurrenceId).ViewRock(rockId);
      var rocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
        .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.ForRock.Id == rockId)
        .List().ToList();
      var now = deleteTime ?? DateTime.UtcNow;

      // Adjusting 'now' to exclude milliseconds, as the current MySQL version rounds dates to the nearest second,
      // which was preventing the use of 'now' for accurate data querying in the database.
      now = now.DateTimeWithoutMilliseconds();
      //if (!rocks.Any()) {
      //	throw new PermissionsException("Goal does not exist.");
      //}

      var currentMeeting = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false);
      if (currentMeeting != null) {
        var mRocks = s.QueryOver<L10Meeting.L10Meeting_Rock>()
          .Where(x => x.DeleteTime == null && x.ForRecurrence.Id == recurrenceId && x.L10Meeting.Id == currentMeeting.Id && x.ForRock.Id == rockId)
          .List().ToList();

        foreach (var r in mRocks) {
          r.DeleteTime = now;
          r.Archived = archived;
          s.Update(r);
        }
      }

      foreach (var r in rocks) {
        r.DeleteTime = now;
        r.Archived = archived;
        s.Update(r);
        //rt.UpdateRecurrences(recurrenceId).Update(
        //	new AngularRecurrence(recurrenceId) {
        //		Rocks = AngularList.CreateFrom(AngularListType.Remove, new AngularRock(r.ForRock.Id))
        //	}
        //);

        //if (r.L10Recurrence.VtoId > 0) {
        //    var vtoId = r.L10Recurrence.VtoId;
        //    var rocksInVTO = s.QueryOver<Vto_Rocks>().Where(x => x.DeleteTime == null && x.Rock.Id == rockId && x.Vto.Id == vtoId).List().ToList();
        //    foreach (var rv in rocksInVTO) {
        //        rv.DeleteTime = now;
        //        s.Update(rv);
        //    }
        //}

        PopulateRockOrigins(s, perm, r.ForRock, true);
        IMeetingRockHookUpdates updates = new IMeetingRockHookUpdates();
        updates.DeleteTime = now;
        await HooksRegistry.Each<IMeetingRockHook>((ss, x) => x.DetachRock(ss, r.ForRock, recurrenceId, updates));
      }




      var rocksInOthers = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.DeleteTime == null && x.ForRock.Id == rockId).RowCount();
      if (rocksInOthers == 0 && archiveWhenNotInMeetings) {
        var rock = s.Get<RockModel>(rockId);
        if (rock.FromTemplateItemId == null) {
          //perms.EditRock was cached.
          perm.SetCache(true, "EditRock", rockId);
          await RockAccessor.ArchiveRock(s, perm, rockId);
        }
      }

      rt.UpdateRecurrences(recurrenceId).Call("recalculatePercentage", rockId);
    }


    public static async Task SetVtoRock(UserOrganizationModel caller, long recurrenceId, long rockId, bool vtoRock) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var recurRocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.ForRock.Id == rockId).Select(x => x.Id).List<long>().ToList();

          foreach (var rr in recurRocks) {
            await SyncUtil.ExecuteNonAtomically(s, perms, new SetVtoRockExecutor(rr, vtoRock));
            //await SetVtoRock(s, perms, rr, vtoRock);
          }

          tx.Commit();
          s.Flush();
        }
      }
    }

    [Obsolete("do not use. use SetVtoRockExecutor instead", true)]

    public static async Task SetVtoRock(ISession s, PermissionsUtility perm, long recurRockId, bool vtoRock) {
      var recurRock = s.Get<L10Recurrence.L10Recurrence_Rocks>(recurRockId);
      perm.EditRock(recurRock.ForRock.Id, false); //perm.EditL10Recurrence(recurRock.L10Recurrence.Id);
      recurRock.VtoRock = vtoRock;
      s.Update(recurRock);

      var meetingId = recurRock.L10Recurrence.MeetingInProgress;
      if (meetingId != null) {
        var meetingRocks = s.QueryOver<L10Meeting.L10Meeting_Rock>().Where(x => x.DeleteTime == null && x.ForRock.Id == recurRock.ForRock.Id && x.L10Meeting.Id == meetingId.Value).List().ToList();
        foreach (var m in meetingRocks) {
          m.VtoRock = vtoRock;
          s.Update(m);
        }
      }
      await HooksRegistry.Each<IMeetingRockHook>((ss, x) => x.UpdateVtoRock(ss, recurRock));
    }

    public static async Task SetVtoRock(UserOrganizationModel caller, long recurRockId, bool vtoRock) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          await SyncUtil.ExecuteNonAtomically(s, perms, new SetVtoRockExecutor(recurRockId, vtoRock));
          tx.Commit();
          s.Flush();
        }
      }
    }

    [Obsolete("Does not check goal safety")]
    private static async Task _AddExistingRockToL10(ISession s, PermissionsUtility perm, long recurrenceId, RockModel rock, DateTime? nowTime = null, bool vtoRock = false) {
      if (rock.Id == 0) {
        throw new Exception("Goal doesn't exist");
      }

      if (rock._AddedToL10 && rock._L10Added.Contains(recurrenceId)) {
        throw new PermissionsException("Already added to Weekly Meeting");
      }

      rock._AddedToL10 = true;
      rock._L10Added.Add(recurrenceId);
      var recur = s.Get<L10Recurrence>(recurrenceId);

      perm.AdminL10Recurrence(recurrenceId);

      var now = nowTime ?? DateTime.UtcNow;

      var rm = new L10Recurrence.L10Recurrence_Rocks() {
        CreateTime = now,
        L10Recurrence = recur,
        ForRock = rock,
        VtoRock = vtoRock
      };
      s.Save(rm);

      var current = L10Accessor._GetCurrentL10Meeting_Unsafe(s, recurrenceId, true, false, false);
      if (current != null) {
        var mm = new L10Meeting.L10Meeting_Rock() {
          ForRecurrence = recur,
          L10Meeting = current,
          ForRock = rock,
          VtoRock = vtoRock,
          Completion = rock.Completion
        };
        s.Save(mm);
      }

      PopulateRockOrigins(s, perm, rock, false);

      var c = perm.GetCaller();
      await HooksRegistry.Each<IMeetingRockHook>((ss, x) => x.AttachRock(ss, c, rock, rm));
    }

    [Untested("make sure the query is working correctly")]
    private static async Task _UpdateMeetingRockCompletionTimes_Unsafe(ISession s, long rockId, RockState? state, DateTime now) {
      if (state != null) {
        L10Recurrence recurA = null;
        var allCurrentMeetingRocks = s.QueryOver<L10Meeting.L10Meeting_Rock>()
                        .JoinAlias(x => x.ForRecurrence, () => recurA)
                        .Where(x => x.DeleteTime == null && x.ForRock.Id == rockId && recurA.MeetingInProgress == x.L10Meeting.Id)
                        .List().ToList();


        foreach (var meetingRock in allCurrentMeetingRocks) {
          if (state != RockState.Indeterminate && meetingRock.Completion != state) {
            if (state == RockState.Complete) {
              meetingRock.CompleteTime = now;
            }
            meetingRock.Completion = state.Value;
            s.Update(meetingRock);
          } else if ((state == RockState.Indeterminate) && meetingRock.Completion != RockState.Indeterminate) {
            meetingRock.Completion = RockState.Indeterminate;
            meetingRock.CompleteTime = null;
            s.Update(meetingRock);
          }
        }
      }
    }

    //public static async Task AttachRock(UserOrganizationModel caller, long recurrenceId, long rockId) {
    //	using (var s = HibernateSession.GetCurrentSession()) {
    //		using (var tx = s.BeginTransaction()) {
    //			using (var rt = RealTimeUtility.Create()) {
    //				var perms = PermissionsUtility.Create(s, caller);

    //				await AddRock(s, perms, recurrenceId, new L10Controller.AddRockVm() {
    //					SelectedRock = rockId
    //				});
    //				tx.Commit();
    //				s.Flush();
    //			}
    //		}
    //	}
    //}
    //[Obsolete("Do not use", true)]
    //private static async Task<RockModel> CreateRock(UserOrganizationModel caller, long recurrenceId, L10Controller.AddRockVm model) {
    //	using (var s = HibernateSession.GetCurrentSession()) {
    //		using (var tx = s.BeginTransaction()) {
    //			var perm = PermissionsUtility.Create(s, caller);
    //			var rock = await AddRock(s, perm, recurrenceId, model);
    //			tx.Commit();
    //			s.Flush();
    //			return rock;
    //		}
    //	}
    //}
    //[Untested("Used CreateRock", "Stop using the AddRock shit.")]
    //private static async Task<RockModel> AddRock(ISession s, PermissionsUtility perm, long recurrenceId, L10Controller.AddRockVm model) {
    //	var recur = s.Get<L10Recurrence>(recurrenceId);
    //	await L10Accessor.Depristine_Unsafe(s, perm.GetCaller(), recur);
    //	s.Update(recur);
    //	var now = DateTime.UtcNow;
    //	RockModel rock;

    //	if (model.SelectedRock == -3) {
    //		//Create new
    //		if (model.Rocks == null)
    //			throw new PermissionsException("You must include a goal to create.");

    //		rock = model.Rocks.SingleOrDefault();
    //		if (rock == null)
    //			throw new PermissionsException("You must include a goal to create.");

    //		//---Removed---
    //		//perm.ViewUserOrganization(rock.ForUserId, false);
    //		//rock.OrganizationId = recur.OrganizationId;
    //		//if (rock.CreateTime == DateTime.MinValue)
    //		//    rock.CreateTime = now;
    //		//rock.AccountableUser = s.Load<UserOrganizationModel>(rock.ForUserId);
    //		//rock.Category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

    //		rock = await RockAccessor.CreateRock(s, perm, rock.ForUserId, rock.Rock);

    //		//---Removed---
    //		//s.Save(rock);
    //		//rock.AccountableUser.UpdateCache(s);
    //		//await HooksRegistry.Each<IRockHook>(x => x.CreateRock(s, rock));
    //	} else {
    //		//Find Existing
    //		rock = s.Get<RockModel>(model.SelectedRock);
    //		if (rock == null)
    //			throw new PermissionsException("Goal does not exist.");
    //		perm.ViewRock(rock.Id);
    //	}
    //	await AddExistingRockToL10(s, perm, recurrenceId, rock, now);
    //	return rock;
    //}

    //[Untested("Realtime updateRockCompletion called for all meetings", "Do we still use completion on MeetingRock?")]
    //public static async Task UpdateRockCompletion(UserOrganizationModel caller, long recurrenceId, long meetingRockId, RockState state, string connectionId) {
    //	using (var s = HibernateSession.GetCurrentSession()) {
    //		using (var tx = s.BeginTransaction()) {
    //			var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
    //			var meetingRock = s.Get<L10Meeting.L10Meeting_Rock>(meetingRockId);
    //			if (meetingRock == null)
    //				throw new PermissionsException("Goal does not exist.");
    //			var now = DateTime.UtcNow;
    //			//var updated = false;
    //			perm.EditRock(meetingRock.ForRock.Id);

    //			await RockAccessor.UpdateRock(s, perm, meetingRock.ForRock.Id, completion: state, now: now);
    //			_UpdateMeetingRockCompletionTimes(s, meetingRock, state, now);

    //			//if (state != RockState.Indeterminate && meetingRock.Completion != state) {
    //			//	if (state == RockState.Complete) {
    //			//		meetingRock.CompleteTime = now;
    //			//	}
    //			//	meetingRock.Completion = state;
    //			//	s.Update(meetingRock);
    //			//} else if ((state == RockState.Indeterminate) && meetingRock.Completion != RockState.Indeterminate) {
    //			//	meetingRock.Completion = RockState.Indeterminate;
    //			//	meetingRock.CompleteTime = null;
    //			//	s.Update(meetingRock);
    //			//}


    //			//---Removed---
    //			//if (state != RockState.Indeterminate && rock.Completion != state) {
    //			//	if (state == RockState.Complete) {
    //			//		rock.CompleteTime = now;
    //			//		rock.ForRock.CompleteTime = now;
    //			//	}
    //			//	rock.Completion = state;
    //			//	rock.ForRock.Completion = state;
    //			//	s.Update(rock);
    //			//	s.Update(rock.ForRock);
    //			//	updated = true;
    //			//} else if ((state == RockState.Indeterminate) && rock.Completion != RockState.Indeterminate) {
    //			//	rock.Completion = RockState.Indeterminate;
    //			//	rock.CompleteTime = null;
    //			//	rock.ForRock.Completion = RockState.Indeterminate;
    //			//	rock.ForRock.CompleteTime = null;
    //			//	s.Update(rock);
    //			//	s.Update(rock.ForRock);
    //			//	updated = true;
    //			//}

    //			if (meetingRock.Completion != state) {
    //				Audit.L10Log(s, caller, recurrenceId, "UpdateRockCompletion", ForModel.Create(meetingRock), "\"" + meetingRock.ForRock.Rock + "\" set to \"" + state + "\"");
    //				tx.Commit();
    //				s.Flush();

    //				//couldn't be moved.. needs recurRockId
    //				var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
    //				hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), connectionId).updateRockCompletion(meetingRockId, state.ToString());

    //				_UpdateRock(meetingRock.ForRock.Id, state, connectionId, s, perm, meetingRock.ForRock, hub, now);
    //			}
    //		}
    //	}
    //}
    //[Untested("Vto_Rocks", "Removed real-time update. Re-add")]
    //private static void _UpdateRock_(long rockId, RockState? state, string connectionId, ISession s, PermissionsUtility perms, RockModel rock, IHubContext hub, DateTime now) {
    //	var rockRecurrences = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
    //						  .Where(x => x.DeleteTime == null && x.ForRock.Id == rockId)
    //						  .List().ToList();
    //	foreach (var r in rockRecurrences) {
    //		var curMeeting = _GetCurrentL10Meeting(s, perms, r.L10Recurrence.Id, true, false, false);
    //		if (curMeeting != null) {
    //			var meetingRock = s.QueryOver<L10Meeting.L10Meeting_Rock>().Where(x => x.DeleteTime == null && x.L10Meeting.Id == curMeeting.Id && x.ForRock.Id == rock.Id).SingleOrDefault();
    //			if (meetingRock != null) {

    //				if (state != RockState.Indeterminate && meetingRock.Completion != state) {
    //					meetingRock.Completion = state.Value;
    //					if (state == RockState.Complete) {
    //						meetingRock.CompleteTime = now;
    //					}
    //					s.Update(meetingRock);
    //				} else if ((state == RockState.Indeterminate) && rock.Completion != RockState.Indeterminate) {
    //					meetingRock.Completion = RockState.Indeterminate;
    //					meetingRock.CompleteTime = null;
    //					s.Update(meetingRock);
    //				}
    //				hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id), connectionId)
    //					.updateRockCompletion(meetingRock.Id, state.ToString(), rock.Id);
    //			}
    //		} else {
    //			hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id), connectionId)
    //				.updateRockCompletion(0, state.ToString(), rock.Id);
    //		}
    //		//hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id), connectionId).update(new AngularUpdate() { new AngularRock(rock) });
    //	}
    //}

    #endregion
  }
}