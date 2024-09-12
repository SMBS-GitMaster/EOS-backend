using Hangfire;
using Microsoft.Extensions.Logging;
using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.ScheduledTasks;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.Hangfire;
using RadialReview.Hangfire.Activator;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Payments;
using RadialReview.Models.Periods;
using RadialReview.Models.Prereview;
using RadialReview.Models.Tasks;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
  public class TaskAccessor : BaseAccessor {
    public static async Task CleanupSyncs_Hangfire(double days) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          try {
            days = Math.Max(days, 0.1);

            var syncTable = "Sync";
            s.CreateSQLQuery("delete from " + syncTable + " where CreateTime < \"" + DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd") + "\"").ExecuteUpdate();
            DeleteOldSyncLocks(s, days);
            tx.Commit();
            s.Flush();

          } catch (Exception e) {
            log.Error(e);
          }
        }
      }
    }

    public static void DeleteOldSyncLocks(ISession s, double days) {
      days = Math.Max(days, 0.1);
      var syncTable = "SyncLock";
      s.CreateSQLQuery("delete from " + syncTable + " where LastClientUpdateTimeMs < \"" + DateTime.UtcNow.AddDays(-days).ToJavascriptMilliseconds() + "\"").ExecuteUpdate();
      //s.CreateSQLQuery("delete from " + syncTable + " where LastUpdateDb < \"" + DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd") + "\"").ExecuteUpdate();
    }



    [Queue(HangfireQueues.Immediate.EXECUTE_TASKS)]
    [AutomaticRetry(Attempts = 0)]
    public static async Task CheckCardExpirations_Hangfire() {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          var date = DateTime.UtcNow.Date;
          if (date == new DateTime(date.Year, date.Month, 1) || date == new DateTime(date.Year, date.Month, 15) || date == new DateTime(date.Year, date.Month, 21)) {
            var expireMonth = date.AddMonths(1);
            var tokens = s.QueryOver<PaymentSpringsToken>()
                .Where(x => x.Active && x.DeleteTime == null && x.TokenType == PaymentSpringTokenType.CreditCard && x.MonthExpire == expireMonth.Month && x.YearExpire == expireMonth.Year)
                .List().ToList();

            var tt = tokens.GroupBy(x => x.OrganizationId).Select(x => x.OrderByDescending(y => y.CreateTime).First());
            foreach (var t in tt) {
              await HooksRegistry.Each<IPaymentHook>((ses, x) => x.CardExpiresSoon(ses, t));
            }
          }

          tx.Commit();
          s.Flush();
        }
      }
    }


    [Queue(HangfireQueues.Immediate.EXECUTE_TASKS)]
    [AutomaticRetry(Attempts = 0)]
    public static async Task<object> EnqueueTasks(DateTime now) {
      var nowV = now;
      var tasks = GetTasksToExecute(nowV, false);

      foreach (var t in tasks) {
        try {
          //Must be started here, otherwise it can be queued up several times
          //Start the task ONLY
          var taskId = t.Id;
          using (var s = HibernateSession.GetCurrentSession()) {
            using (var tx = s.BeginTransaction()) {
              var task = s.Get<ScheduledTask>(taskId);
              if (task.Executed != null) {
                continue;//Already executed
              }
              if (task.Started != null) {
                continue; //Already started
              }
              task.Started = nowV;
              tx.Commit();
              s.Flush();
            }
          }
          Scheduler.Enqueue(() => ExecuteTask(t.Id, nowV, false, default(IHttpClientFactory), default(ILogger<TaskAccessor>)));
        } catch (Exception e) {
          log.Error("ExecuteTask - Task execution exception.", e);
        }
      }

      return new {
        Number = tasks.Count
      };
    }


    [Obsolete("Used only in testing")]
    public static async Task<ExecutionResult> EnqueueTask_Test(ScheduledTask t, DateTime now, IHttpClientFactory factory, ILogger<TaskAccessor> logger) {
      var output = new List<ExecutionResult>();
      try {
        //Must be started here, otherwise it can be queued up several times
        //Start the task ONLY
        var taskId = t.Id;
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var task = s.Get<ScheduledTask>(taskId);
            if (task.Executed != null) {
              return null;//Already executed
            }
            if (task.Started != null) {
              return null; //Already started
            }
            task.Started = now;
            tx.Commit();
            s.Flush();
          }
        }
        return await ExecuteTask(t.Id, now, true, factory, logger);
      } catch (Exception e) {
        log.Error("ExecuteTask - Task execution exception.", e);
        throw;
      }
    }

    [Queue(HangfireQueues.Immediate.EXECUTE_TASK)]
    [AutomaticRetry(Attempts = 0)]
    public static async Task<ExecutionResult> ExecuteTask(long taskId, DateTime now, bool test, [ActivateParameter] IHttpClientFactory factory, [ActivateParameter] ILogger<TaskAccessor> logger) {
      var start = DateTime.UtcNow;
      string taskUrl = null;
      ExecutionStatus status = ExecutionStatus.Unstarted;
      List<Exception> errors = new List<Exception>();
      bool errorEmailSent = false;
      var emailsToSend = new List<Mail>();
      var createdTasks = new List<ScheduledTask>();
      dynamic response = null;

      #region Execute Task
      try {
        ScheduledTask task;
        //Execute the task and update the task ONLY
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            task = s.Get<ScheduledTask>(taskId);
            taskUrl = task.Url;
            if (task.Executed != null) {
              errors.Add(new Exception("Task already executed"));
              return new ExecutionResult(taskId, status, taskUrl, response, start, DateTime.UtcNow, createdTasks, errors, errorEmailSent);
            }

            //Error Handling
            if (task.Started == null) {
              errors.Add(new Exception("Task was not started"));
              return new ExecutionResult(taskId, status, taskUrl, response, start, DateTime.UtcNow, createdTasks, errors, errorEmailSent);
            }
            if (new TimeSpan(Math.Abs((task.Started.Value- now).Ticks)) > TimeSpan.FromSeconds(1)) {
              //Somehow it was started in another task.. this one is not ours.
              errors.Add(new Exception("Task already started"));
              return new ExecutionResult(taskId, status, taskUrl, response, start, DateTime.UtcNow, createdTasks, errors, errorEmailSent);
            }

          }
        }


        //Execute function
        ExecuteTaskFunc func;
        //if (test) {
        //	func = d_ExecuteTaskFunc_Test;
        //} else {
        //	func = d_ExecuteTaskFunc;
        //}
        status |= ExecutionStatus.Started;
        //Heavy Lifting...
        var results = await ExecuteTask_Internal(task, now, d_ExecuteTaskFunc, test, factory, logger);


        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            response = results.Response;
            errorEmailSent = results.ExceptionEmailSent;
            errors.AddRange(results.Errors);
            emailsToSend.AddRange(results.SendEmails);
            createdTasks.AddRange(results.CreateTasks);

            s.Update(task);
            tx.Commit();
            s.Flush();

            if (results.Executed) {
              status |= ExecutionStatus.Executed;
            } else {
              errors.Add(new Exception("Task failed to execute"));
              return new ExecutionResult(taskId, status, taskUrl, response, start, DateTime.UtcNow, createdTasks, errors, errorEmailSent);
            }
          }
        }
      } catch (Exception e) {
        log.Error("ExecuteTask - Task execution exception.", e);
        errors.Add(e);
      }
      #endregion
      #region Unmark started
      try {
        //Update started ONLY
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var task = s.Get<ScheduledTask>(taskId);
            task.Started = null;
            s.Update(task);
            tx.Commit();
            s.Flush();
          }
        }
        status |= ExecutionStatus.Unmarked;
      } catch (Exception e) {
        log.Error("ExecuteTask - Task Remove Started exception.", e);
        errors.Add(e);
      }
      #endregion
      #region Save new tasks
      try {
        //Add newly created tasks ONLY
        var allFailed = false;
        if (createdTasks.Any()) {
          allFailed = true;
          using (var s = HibernateSession.GetCurrentSession()) {
            using (var tx = s.BeginTransaction()) {

              var existing = s.QueryOver<ScheduledTask>()
                .WhereRestrictionOn(x => x.CreatedFromTaskId)
                .IsIn(createdTasks.Select(x => x.CreatedFromTaskId).ToArray())
                .List().GroupBy(x => x.CreatedFromTaskId)
                .ToDefaultDictionary(x => x.Key, x => x.ToList(), x => new List<ScheduledTask>());

              foreach (var c in createdTasks) {
                var found = existing[c.CreatedFromTaskId];

                if (!found.Any(x => x.Url == c.Url && (int)(x.Fire.ToJsMs() / 1000) == (int)(c.Fire.ToJsMs() / 1000))) {
                  s.Save(c);
                  allFailed = false;
                } else {
                  log.Error("ExecuteTask - Prevented duplicate task from being added");
                }
              }
              tx.Commit();
              s.Flush();
            }
          }
        }
        if (!allFailed) {
          status |= ExecutionStatus.TasksCreated;
        }
      } catch (Exception e) {
        log.Error("ExecuteTask - Task Creation exception.", e);
        errors.Add(e);
      }
      #endregion
      #region Send emails
      try {
        if (emailsToSend.Any()) {
          log.Info("ExecuteTasks - Sending (" + emailsToSend.Count + ") emails " + DateTime.UtcNow);
          await Emailer.SendEmails(emailsToSend);
          log.Info("ExecuteTasks - Done sending emails " + DateTime.UtcNow);
        }
        status |= ExecutionStatus.EmailsSent;
      } catch (Exception e) {
        log.Error("ExecuteTasks - Task execution exception. Email failed (2).", e);
        errors.Add(e);
      }

      #endregion
      var result = new ExecutionResult(taskId, status, taskUrl, response, start, DateTime.UtcNow, createdTasks, errors, errorEmailSent);
      if (!result.Status.HasFlag(ExecutionStatus.Executed) && result.HasError) {
        throw result.ToException();
      }
      return result;

    }



    protected delegate Task<dynamic> ExecuteTaskFunc(ScheduledTaskHandlerData data);

    protected static async Task<TaskResult> ExecuteTask_Internal(ScheduledTask task, DateTime now, ExecuteTaskFunc execute, bool isTest, IHttpClientFactory factory, ILogger logger) {
      var o = new TaskResult();
      var newTasks = o.CreateTasks;

      if (task != null) {
        try {
          if (task.Url != null) {
            try {
              var data = createScheduledTaskHandlerData(task, now, isTest, factory, logger);
              o.Response = await execute(data);
            } catch (WebException webEx) {
              var response = webEx.Response as HttpWebResponse;
              if (response != null && response.StatusCode == HttpStatusCode.NotImplemented) {
                //Fallthrough Exception...
                log.Info("Task Fallthrough [OK] (taskId:" + task.Id + ") (url:" + task.Url + ")");
              } else {
                throw webEx;
              }
            }
          }
          log.Debug("Scheduled task was executed. " + task.Id);
          task.Executed = DateTime.UtcNow;
          o.Executed = true;
          if (task.NextSchedule != null) {
            var nt = new ScheduledTask() {
              FirstFire = (task.FirstFire ?? task.Fire).AddTimespan(task.NextSchedule.Value),
              Fire = (task.FirstFire ?? task.Fire).AddTimespan(task.NextSchedule.Value),
              NextSchedule = task.NextSchedule,
              Url = task.Url,
              TaskName = task.TaskName,
              MaxException = task.MaxException,
              OriginalTaskId = task.OriginalTaskId,
              CreatedFromTaskId = task.Id,
              EmailOnException = task.EmailOnException,
            };
            while (nt.Fire < DateTime.UtcNow) {
              nt.Fire = nt.Fire.AddTimespan(task.NextSchedule.Value);
              if (nt.FirstFire != null) {
                nt.FirstFire = nt.FirstFire.Value.AddTimespan(task.NextSchedule.Value);
              }
            }

            newTasks.Add(nt);
          }
        } catch (Exception e) {
          o.Errors.Add(e);
          log.Error("Scheduled task error. " + task.Id, e);

          //Send an email
          if (task != null && task.EmailOnException) {
            try {
              var builder = new StringBuilder();
              builder.AppendLine("TaskId:" + task.Id + "<br/>");
              if (e != null) {
                builder.AppendLine("ExceptionType:" + e.GetType() + "<br/>");
                builder.AppendLine("Exception:" + e.Message + "<br/>");
                builder.AppendLine("ExceptionCount:" + task.ExceptionCount + " out of " + task.MaxException + "<br/>");
                builder.AppendLine("Url:" + task.Url + "<br/>");
                builder.AppendLine("StackTrace:<br/>" + (e.StackTrace ?? "").Replace("\n", "\n<br/>") + "<br/>");
              } else {
                builder.AppendLine("Exception was null <br/>");
              }
              var mail = Mail.To(EmailTypes.CustomerSupport, ProductStrings.EngineeringEmail)
                .SubjectPlainText("Task failed to execute. Action Required.")
                .BodyPlainText(builder.ToString());

              o.SendEmails.Add(mail);
              o.ExceptionEmailSent = true;
            } catch (Exception ee) {
              o.Errors.Add(ee);
              log.Error("Task execution exception. Email failed (1).", ee);
            }
          }
          task.ExceptionCount++;
          if (task.MaxException != null && task.ExceptionCount >= task.MaxException) {
            if (task.NextSchedule != null) {
              newTasks.Add(new ScheduledTask() {
                FirstFire = (task.FirstFire ?? task.Fire).Add(task.NextSchedule.Value),
                Fire = (task.FirstFire ?? task.Fire).Add(task.NextSchedule.Value),
                NextSchedule = task.NextSchedule,
                Url = task.Url,
                MaxException = task.MaxException,
                TaskName = task.TaskName,
                OriginalTaskId = task.OriginalTaskId,
                CreatedFromTaskId = task.Id,
              });
              task.Executed = DateTime.MaxValue;
            }
          }
          task.Fire = DateTime.UtcNow + TimeSpan.FromMinutes(5 + Math.Pow(2, task.ExceptionCount + 1));
        }
      }
      return o;
    }

    private static ScheduledTaskHandlerData createScheduledTaskHandlerData(ScheduledTask task, DateTime now, bool isTest, IHttpClientFactory factory, ILogger logger) {
      var url = "";
      var server = Config.BaseUrl(null);
      if (server != null) {
        url = (server.TrimEnd('/')) + "/";
      }
      url = url + task.Url.TrimStart('/');
      if (url.Contains("?")) {
        url += "&taskId=" + task.Id;
      } else {
        url += "?taskId=" + task.Id;
      }
      var uri = new Uri(url, UriKind.Absolute);
      var data = new ScheduledTaskHandlerData() {
        ClientFactory = factory,
        IsTest = isTest,
        Now = now,
        TaskId = task.Id,
        TaskName = task.TaskName,
        Url = uri,
        Logger = logger
      };
      return data;
    }

    public static async Task<dynamic> d_ExecuteTaskFunc(ScheduledTaskHandlerData data) {
      foreach (var h in ScheduledTaskRegistry.Handlers) {
        if (h.ShouldHandle(data)) {
          await h.Handle(data);
          return true;
        }
      }
      throw new NotImplementedException("Unhandled task");
    }

    protected static async Task<dynamic> d_ExecuteTaskFunc_Test(ScheduledTaskHandlerData data) {// string _unused, ScheduledTask task, DateTime now, IHttpClientFactory factory) {
      var path = data.Url.AbsolutePath;
      var pathParts = path.Split('/');

      if (path.StartsWith("/Scheduler/ChargeAccount/")) {
        return await PaymentAccessor.EnqueueChargeOrganizationFromTask(pathParts.Last().ToLong(), data.TaskId, true, executeTime: data.Now);
      } else if (data.Url.AbsoluteUri == "https://example.com") {
        using (var webClient = data.ClientFactory.CreateClient()) {
          return await webClient.GetStringAsync(data.Url);
        }
      } else {
        throw new Exception("Unhandled URL: " + data.Url.AbsoluteUri);
      }
    }


    public static List<ScheduledTask> GetTasksToExecute(DateTime now, bool markStarted) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction(IsolationLevel.Serializable)) {
          var current = s.QueryOver<ScheduledTask>().Where(x => x.Executed == null && x.Started == null && now >= x.Fire && x.DeleteTime == null && x.ExceptionCount <= 11 && (x.MaxException == null || x.ExceptionCount < x.MaxException)).List()
            .Where(x => x.ExceptionCount < (x.MaxException ?? 12))
            .ToList();

          if (markStarted) {
            var d = DateTime.UtcNow;
            foreach (var c in current) {
              c.Started = d;
            }
            tx.Commit();
            s.Flush();
          }

          return current;
        }
      }
    }


    public static long AddTask(AbstractUpdate update, ScheduledTask task) {
      update.Save(task);
      task.OriginalTaskId = task.Id;
      update.Update(task);
      return task.Id;
    }

    public static long AddTask(ScheduledTask task) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var output = AddTask(s.ToUpdateProvider(), task);
          tx.Commit();
          s.Flush();
          return output;
        }
      }
    }

    public static List<TaskModel> GetTasksForUser(UserOrganizationModel caller, long forUserId, DateTime now) {
      var tasks = new List<TaskModel>();
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);

          //Reviews
          var reviews = ReviewAccessor
            .GetReviewsForUser(s, perms, caller, forUserId, 0, int.MaxValue, now)
            .ToListAlive()
            .GroupBy(x => x.ForReviewContainerId);

          var reviewTasks = reviews.Select(x => new TaskModel() {
            Id = x.First().ForReviewContainerId,
            Type = TaskType.Review,
            Completion = CompletionModel.FromList(x.Select(y => y.GetCompletion())),
            DueDate = x.Max(y => y.DueDate),
            Name = x.First().Name
          });
          tasks.AddRange(reviewTasks);

          //Prereviews
          var prereviews = PrereviewAccessor.GetPrereviewsForUser(s.ToQueryProvider(true), perms, forUserId, now)
            .Where(x => x.Executed == null).ToListAlive();
          var reviewContainers = new Dictionary<long, String>();
          var prereviewCount = new Dictionary<long, int>();
          foreach (var p in prereviews) {
            reviewContainers[p.ReviewContainerId] = ReviewAccessor.GetReviewContainer(s.ToQueryProvider(true), perms, p.ReviewContainerId).ReviewName;
            prereviewCount[p.Id] = s.QueryOver<PrereviewMatchModel>()
              .Where(x => x.PrereviewId == p.Id && x.DeleteTime == null)
              .RowCount();
          }
          var prereviewTasks = prereviews.Select(x => new TaskModel() {
            Id = x.Id,
            Type = TaskType.Prereview,
            Count = prereviewCount[x.Id],
            DueDate = x.PrereviewDue,
            Name = reviewContainers[x.ReviewContainerId]
          });
          tasks.AddRange(prereviewTasks);
          var todos = TodoAccessor.GetTodosForUser(caller, caller.Id).Where(x =>
            (x.CompleteTime == null && x.DueDate < DateTime.UtcNow.AddDays(7))
          ).ToList();

          var todoTasks = todos.Select(x => new TaskModel() {
            Id = x.Id,
            Type = TaskType.Todo,
            DueDate = x.DueDate ?? DateTime.UtcNow.AddDays(7),
            Name = x.Name,
          });
          tasks.AddRange(todoTasks);


          try {
            if (String.IsNullOrEmpty(s.Get<UserOrganizationModel>(forUserId).User.ImageGuid)) {
              tasks.Add(new TaskModel() {
                Type = TaskType.Profile,
                Name = "Update Profile (Picture)",
                DueDate = DateTime.MaxValue,
              });
            }
          } catch {

          }



        }
      }
      return tasks;
    }

    public static void UpdateScorecard(DateTime now) {





    }

    public static void EnsureTaskIsExecuting(ISession s, long taskId) {
      var task = s.Get<ScheduledTask>(taskId);
      if (task.Executed != null) {
        throw new PermissionsException("Task was already executed.");
      }

      if (task.DeleteTime != null) {
        throw new PermissionsException("Task was deleted.");
      }

      if (task.OriginalTaskId == 0) {
        throw new PermissionsException("ScheduledTask OriginalTaskId was 0.");
      }

      if (task.Started == null) {
        throw new PermissionsException("Task was not started.");
      }
    }


    [Queue(HangfireQueues.Immediate.CLOSE_MEETING)]
    [AutomaticRetry(Attempts = 0)]
    public static async Task<bool> CloseMeeting(long recurrenceId, [ActivateParameter] INotesProvider notesProvider) {
      var leaderId = await L10Accessor.GetMeetingLeader(UserOrganizationModel.ADMIN, recurrenceId);
      var leader = leaderId == null ? UserOrganizationModel.ADMIN : UserAccessor.Unsafe.GetUserOrganizationById(leaderId.Value);
      await L10Accessor.ConcludeMeeting(leader, notesProvider, recurrenceId, new List<Tuple<long, decimal?>>(), ConcludeSendEmail.None, false, false, null);
      return true;
    }
  }
}
