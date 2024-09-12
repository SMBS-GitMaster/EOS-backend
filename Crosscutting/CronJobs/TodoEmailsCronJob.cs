using Hangfire;
using NHibernate;
using NHibernate.SqlCommand;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Todo;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Middleware.Services.NotesProvider;
using Microsoft.Extensions.Logging;
using Hangfire.Server;
using Hangfire.Console;
using RadialReview.Crosscutting.Hangfire.Jobs;
using RadialReview.Hangfire;
using NHibernate.Criterion;
using Hangfire.Batches;
using RadialReview.Crosscutting.Schedulers;
using EmailStrings = RadialReview.Core.Properties.EmailStrings;
using RadialReview.Models.L10;

namespace RadialReview.Crosscutting.CronJobs
{
  public class TodoEmailsScheduler : ICronJob
  {
    public CronJobBehavior Behavior => new CronJobBehavior(true, "TodoEmailReminder_v2", Cron.Hourly(54), GapExecutionBehavior.OnlyPast24Hours);

    private static int DIVISOR = 61; //May be a good idea to be prime
    private static int MAX_TODOS_PER_GROUP = 40;


    private INotesProvider _notesProvider;
    private ILogger<TodoEmailsScheduler> _logger;
    private PerformContext _console;
    private JobInfo _jobInfo;

    private static bool DEBUG_RUN_ALL = false;

    public TodoEmailsScheduler(INotesProvider notesProvider, ILogger<TodoEmailsScheduler> logger, JobInfo jobInfo)
    {
      _notesProvider = notesProvider;
      _logger = logger;
      _jobInfo = jobInfo;
    }

    public async Task Execute(DateTime executionTime)
    {
      var currentHour = ((int)Math.Round((executionTime - executionTime.Date).TotalHours)) % 24;

      //var todoReminderBatchId = BatchJob.StartNew(batch => { }, "Todo reminder email hour " + currentHour);
      //BatchJob.Attach(batchId, batch => {
      for(var remainder = 0; remainder < DIVISOR; remainder++)
      {
        Scheduler.Enqueue(() => ConstructAndSendModulo(/*batchId,*/ executionTime, currentHour, remainder, DIVISOR, default(ILogger<TodoEmailsScheduler>), default(INotesProvider), default(PerformContext)));
      }
      //});//, "Generate Todo Reminder Emails", options: BatchContinuationOptions.OnAnyFinishedState);
    }



    [Queue(HangfireQueues.Immediate.REMINDER_EMAILS)]
    public async Task ConstructAndSendModulo(/*string batchId,*/ DateTime executionTime, int currentHour, int remainder, int divisor, ILogger<TodoEmailsScheduler> logger, INotesProvider notesProvider, PerformContext console)
    {
      _console.WriteLine(string.Format("Constructing todo emails: {0,5}/{1,-5}", remainder, divisor - 1));
      List<Mail> unsent;
      using(var s = HibernateSession.GetCurrentSession())
      {
        using(var tx = s.BeginTransaction())
        {
          unsent = await _ConstructTodoEmails(_notesProvider, currentHour, s, executionTime, _logger, remainder, divisor);
        }
      }

      _console.WriteLine("  - Scheduling " + unsent.Count() + " emails");
      if(unsent.Any())
      {
        //BatchJob.Attach(batchId, batch => {
        foreach(var m in unsent)
        {
          Scheduler.Enqueue(() => Emailer.SendEmail(m, false, true));
        }
        //});//, string.Format("Send Todo Reminder Emails: {0,5}/{1,-5}", remainder, divisor - 1), BatchContinuationOptions.OnAnyFinishedState);

      }
    }


    #region Build emails
    /// <summary>
    /// remainder less than divisor
    /// </summary>
    private static async Task<List<Mail>> _ConstructTodoEmails(INotesProvider notesProvider, int currentTime, ISession s, DateTime nowUtc, ILogger<TodoEmailsScheduler> logger, int remainder, int divisor)
    {
      var minimumSearchRange = nowUtc.Date.AddDays(-30);
      Dictionary<string, List<TinySchedulerTodo>> dictionary = _GetAllTodosForSendTime(s, currentTime, nowUtc, remainder, divisor, minimumSearchRange);

      var unsent = new List<Mail>();
      foreach(var userTodos in dictionary)
      {
        try
        {
          var emails = await _ConstructTodoEmail(notesProvider, currentTime, nowUtc, minimumSearchRange, userTodos.Value, logger);
          unsent.AddRange(emails);
        }
        catch(Exception e)
        {
          logger.LogError("Todo reminder email error", e);
        }
      }
      return unsent;
    }

    private static Dictionary<string, List<TinySchedulerTodo>> _GetAllTodosForSendTime(ISession s, int currentTime, DateTime nowUtc, int remainder, int divisor, DateTime minimumSearchRange)
    {
      var tomorrow = nowUtc.Date.AddDays(2).AddTicks(-1);
      var rangeLow = nowUtc.Date.AddDays(-1);
      var rangeHigh = nowUtc.Date.AddDays(4).AddTicks(-1);
      var nextWeek = nowUtc.Date.AddDays(7);
      if(nowUtc.DayOfWeek == DayOfWeek.Friday)
        rangeHigh = rangeHigh.AddDays(1);

      var todos = _QueryTodo(s, currentTime, remainder, divisor, rangeLow, rangeHigh, nextWeek, minimumSearchRange);

      var dictionary = new Dictionary<string, List<TinySchedulerTodo>>();
      foreach(var t in todos.Where(x => x.AccountableUserEmail != null).GroupBy(x => x.AccountableUserEmail))
      {
        if(t.Key != null)
        {
          dictionary.GetOrAddDefault(t.Key, x => new List<TinySchedulerTodo>()).AddRange(t);
        }
      }

      return dictionary;
    }

    private static List<TinySchedulerTodo> _QueryTodo(ISession s, int currentTime, long remainder, long divisor, DateTime rangeLow, DateTime rangeHigh, DateTime nextWeek, DateTime minimumSearchRange)
    {

      UserOrganizationModel accUser = null;
      UserModel userModel = null;
      OrganizationModel org = null;

      var query = s.QueryOver<TodoModel>()
              .JoinAlias(x => x.AccountableUser, () => accUser, JoinType.LeftOuterJoin)
              .JoinAlias(x => accUser.User, () => userModel, JoinType.LeftOuterJoin)
              .JoinAlias(x => accUser.Organization, () => org, JoinType.LeftOuterJoin)
              .Where(x => x.DeleteTime == null && org.DeleteTime == null && accUser.DeleteTime == null)
              .Where(x => ((rangeLow <= x.DueDate && x.DueDate <= rangeHigh) || (x.CompleteTime == null && /*minimumSearchRange <= x.DueDate [Keep these so we can show them if a newer todo is due.] &&*/ x.DueDate <= nextWeek)))
              .Where(Restrictions.Eq(Projections.SqlFunction("mod", NHibernateUtil.Int64, Projections.Property<TodoModel>(x => x.AccountableUserId), Projections.Constant(divisor)), remainder));
      //query = query.Where(Restrictions.On(() => accUser).IsNotNull);
      //query = query.Where(Restrictions.On(() => userModel).IsNotNull);
      if(!DEBUG_RUN_ALL)
      {
        query = query.Where(x => userModel.SendTodoTime == currentTime);
      }
      query = query.Select(
        x => x.Id, x => x.Message, x => userModel.UserName,
        x => x.DueDate, x => x.CompleteTime, x => x.ForRecurrenceId,
        x => x.PadId, x => x.AccountableUser.Id, x => org._Settings.TimeZoneId,
        x => userModel.FirstName, x => userModel.LastName, x => org._Settings.DateFormat,
        x => userModel.SendTodoTime, x => org.Name
      );

      return query.List<object[]>().Select(x => {
        var id = (long)x[0];
        var message = (string)x[1];
        var accountableUserEmail = (string)x[2];
        var dueDate = (DateTime)x[3];
        var completeTime = (DateTime?)x[4];
        var forRecurrenceId = (long?)x[5];
        var padId = (string)x[6];
        var accountableUserId = (long)x[7];
        var timeZoneOffset = TimeData.GetTimezoneOffset((string)x[8]);
        var userFirstName = (string)x[9];
        var userLastName = (string)x[10];
        var dateFormat = (string)x[11];
        var sendTimeKey = (int?)x[12];
        var organizationName = (string?)x[13];
        var meetingName = "Personal";

        if(forRecurrenceId != 0 && forRecurrenceId != null)
        {
          var queryMeeting = s.Query<L10Recurrence>().Where(x => x.Id == forRecurrenceId).ToList();
          if(queryMeeting.Count > 0)
          {
            meetingName = queryMeeting[0].Name;
          }
        }

        var userFirstLastName = ((userFirstName + " " + userLastName) ?? "").Trim();

        return new TinySchedulerTodo(
          id, message, accountableUserEmail, dueDate, completeTime, null/*TODO get origin name*/,
          forRecurrenceId, padId, accountableUserId, sendTimeKey, userFirstLastName,
          timeZoneOffset, dateFormat, organizationName, meetingName
        );
      }).ToList();
    }

    private static async Task<List<Mail>> _ConstructTodoEmail(INotesProvider notesProvider, int currentTime, DateTime nowUtc, DateTime minimumSearchRange, List<TinySchedulerTodo> userTodos, ILogger<TodoEmailsScheduler> logger)
    {
      string subject = null;
      var nowLocal = TimeData.ConvertFromServerTime(nowUtc, userTodos.FirstOrDefault().NotNull(x => x.AccountableUserTimezoneOffset));// userTodos.First().Organization.ConvertFromUTC(nowUtc).Date;

      var overDue = userTodos.Count(x => x.DueDate.Date <= nowLocal.Date.AddDays(-1) && x.CompleteTime == null);
      if(overDue == 1)
        subject = "You have an overdue to-do";
      else if(overDue > 1)
        subject = "You have " + overDue + " overdue to-dos";
      else
      {
        var dueToday = userTodos.Count(x => x.DueDate.Date == nowLocal.Date && x.CompleteTime == null);

        if(dueToday == 1)
          //subject = "You have a to-do due today";
          subject = "Here is your To-do Email Reminder";
        else if(dueToday > 1)
          //subject = "You have " + dueToday + " to-dos due today";
          subject = "Here is your To-do Email Reminder";
        else
        {
          var dueTomorrow = userTodos.Count(x => x.DueDate.Date == nowLocal.AddDays(1).Date && x.CompleteTime == null);
          if(dueTomorrow == 1)
            //subject = "You have a to-do due tomorrow";
            subject = "Here is your To-do Email Reminder";
          else if(dueTomorrow > 1)
            //subject = "You have " + dueTomorrow + " to-dos due tomorrow";
            subject = "Here is your To-do Email Reminder";
          else
          {
            var dueSoon = userTodos.Count(x => x.DueDate.Date > nowLocal.AddDays(1).Date && x.CompleteTime == null);
            if(dueSoon == 1)
              subject = "You have a to-do due soon";
            else if(dueSoon > 1)
              subject = "You have " + dueSoon + " to-dos due soon";
          }
        }
      }

      //Prevents emails from being sent on todos overdue by 30 days.
      var shouldSend = userTodos.Count(x => minimumSearchRange.Date <= x.DueDate.Date && x.CompleteTime == null);

      if(DEBUG_RUN_ALL || (subject != null && shouldSend > 0))
      {

        try
        {
          //var user = userTodos.First().AccountableUser;

          if(DEBUG_RUN_ALL || userTodos.First().SendTime == currentTime)
          {
            var first = userTodos.First();
            var email = first.AccountableUserEmail;
            var name = first.AccountableUserName;
            var tzOffset = first.AccountableUserTimezoneOffset;
            var dateFormat = first.AccountableUserDateFormat;
            var organizationName = first.OrganizationName;

            var builder = new StringBuilder();
            foreach(var t in userTodos.Where(x => x.CompleteTime == null || x.DueDate.Date > nowUtc.Date).GroupBy(x => x.OriginId))
            {
              var todos = t.Cast<ITodoTiny>().ToList();
              var todosSlim = todos.Take(MAX_TODOS_PER_GROUP).ToList();

              var table = await TodoAccessor.BuildTodoTable(notesProvider, todosSlim, tzOffset, dateFormat, t.First().OriginName.NotNull(x => x + " To-do"), meetingName: t.First().MeetingName);
              builder.Append(table);

              var overflow = todos.Count - todosSlim.Count;
              if(overflow > 0)
              {
                builder.Append("<span><i>and " + overflow + " more...</i></span>");
              }

              builder.Append("<br/>");
            }

            var mail = Mail.To(EmailTypes.DailyTodo, email)
              .Subject(EmailStrings.TodoReminder_Subject, subject)
              .Body(EmailStrings.TodoReminder_Body,
                name,
                builder.ToString(),
                Config.ProductName(null),
                Config.BaseUrl(null) + "Todo/List",
                organizationName,
                Config.BaseUrl(null)
              );

            return mail.AsList();
          }
        }
        catch(Exception e)
        {
          int a = 0;
          logger.LogError("ConstructTodoEmail Error", e);
        }
      }
      return new List<Mail>();
    }

    public class TinySchedulerTodo : ITodoTiny
    {
      public TinySchedulerTodo(long id, string message, string accountableUserEmail, DateTime dueDate, DateTime? completeTime, string originName, long? originId, string padId, long accountableUserId, int? sendTimeKey, string accountableUserName, int accountableUserTimezoneOffset, string accountableUserDateFormat, string organizationName, string meetingName)
      {
        Id = id;
        Message = message;
        AccountableUserEmail = accountableUserEmail;
        DueDate = dueDate;
        CompleteTime = completeTime;
        OriginName = originName;
        OriginId = originId;
        PadId = padId;
        AccountableUserId = accountableUserId;
        SendTime = sendTimeKey;
        AccountableUserName = accountableUserName;
        AccountableUserTimezoneOffset = accountableUserTimezoneOffset;
        AccountableUserDateFormat = accountableUserDateFormat;
        OrganizationName = organizationName;
        MeetingName = meetingName;
      }

      public long Id { get; set; }
      public string Message { get; set; }
      public string AccountableUserEmail { get; set; }
      public DateTime DueDate { get; set; }
      public DateTime? CompleteTime { get; set; }
      public string OriginName { get; set; }
      public long? OriginId { get; set; }
      public string PadId { get; set; }
      public long AccountableUserId { get; set; }
      public int? SendTime { get; set; }
      public string AccountableUserName { get; set; }
      public int AccountableUserTimezoneOffset { get; set; }
      public string AccountableUserDateFormat { get; set; }
      public string OrganizationName { get; set; }
      public string MeetingName { get; set; }
    }
    #endregion
  }
}