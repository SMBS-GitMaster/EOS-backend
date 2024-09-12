using Hangfire;
using Microsoft.AspNetCore.Html;
using RadialReview.Hangfire;
using RadialReview.Hangfire.Activator;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.Application;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Todo;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static RadialReview.Accessors.L10Accessor.MeetingSummaryEmailData;
using RadialReview.Core.Models.Terms;
using RadialReview.Core.Accessors;
using NHibernate.Criterion;
using NHibernate;
using static RadialReview.Models.Issues.IssueModel;
using RadialReview.Variables;

namespace RadialReview.Accessors {
  public partial class L10Accessor : BaseAccessor {
    public class EmailConclusionData {
      public EmailConclusionData(string owner, string name, string date, string description = null) {
        Owner = owner;
        Date = date;
        Name = name;
        Description = description;
      }

      public string Owner { get; }
      public string Date { get; }
      public string Name { get; }
      public string Description { get; }
    }

    public class MeetingSummaryEmailData {
      public long? UserId { get; set; }
      public string UsersName { get; set; }
      public string ProductName { get; set; }
      public bool ShowDetails { get; set; }
      public int TimeZoneOffset { get; set; }
      public long RecurrenceId { get; set; }
      public Ratio TodoCompletion { get; set; }
      public Ratio AverageMeetingRating { get; set; }
      public DateTime? StartTime { get; set; }
      public DateTime? CompleteTime { get; set; }
      public DateTime Now { get; set; }
      public long MeetingId { get; set; }
      public bool HasAudio { get; set; }
      public long? WhiteboardFileId { get; set; }

      public TermsCollection Terms { get; set; }

      public class Duration {
        public string Unit { get; set; }
        public string Number { get; set; }
      }



      public Duration GetDuration() {
        var startTime = "...";
        var endTime = "...";
        var duration = "not wrapped up";

        var ellapse = "";
        var unit = "";

        if (StartTime != null) {
          startTime = TimeData.ConvertFromServerTime(StartTime.Value, TimeZoneOffset).ToString("HH:mm");
        }

        if (CompleteTime != null) {
          endTime = TimeData.ConvertFromServerTime(CompleteTime.Value, TimeZoneOffset).ToString("HH:mm");
        }

        if (CompleteTime != null && StartTime != null) {
          var durationMins = (CompleteTime.Value - StartTime.Value).TotalMinutes;
          var durationSecs = (CompleteTime.Value - StartTime.Value).TotalSeconds;

          if (durationMins < 0) {
            duration = "";
            ellapse = "1";
            unit = "Minute";
          } else if (durationMins > 1) {
            duration = (int)durationMins + " minute".Pluralize((int)durationMins);
            ellapse = "" + (int)Math.Max(1, durationMins);
            unit = "Minute".Pluralize((int)durationMins);
          } else {
            ellapse = "" + (int)Math.Max(1, durationSecs);
            duration = (int)(durationSecs) + " second".Pluralize((int)durationSecs);
            unit = "Second".Pluralize((int)durationSecs);
          }
        }
        return new Duration() {
          Number = ellapse,
          Unit = unit
        };

      }
      public DefaultDictionary<string, HtmlString> PadLookup = new DefaultDictionary<string, HtmlString>(x => new HtmlString(""));


      public MeetingSummaryEmailData() {
        Now = DateTime.UtcNow;
      }

      public class MeetingNote
      {
        public MeetingNote(string title, HtmlString content, double? timeStamp)
        {
          Title = title;
          Content = content;
          Date = GetDate(timeStamp);
        }
        public string Title { get; set; }
        public HtmlString Content { get; set; }
        public string Date { get; set; }
        private string GetDate(double? value)
        {
          double defaultValue = 0.0;
          long timeStamp = (long)(value ?? defaultValue);
          DateTimeOffset currentDate = DateTimeOffset.FromUnixTimeSeconds(timeStamp).Date;
          DateTimeOffset today = DateTimeOffset.Now.Date;
          DateTimeOffset yesterday = today.AddDays(-1);

          if (currentDate == today)
          {
            return "Today";
          }
          else if (currentDate == yesterday)
          {
            return "Yesterday";
          }
          else
          {
            return currentDate.ToString("MM/d/yyyy");
          }
        }
      }

      public MeetingSummaryEmailData(ConclusionEmailSettings settings, string dateFormat, int timezoneOffset, string email, TermsCollection terms, string name = null, long? uid = null, bool? isToV3 = false) {
        DateFormat = dateFormat;
        Email = email;
        Attendees = settings.ConclusionItem.SendEmailsToAttendees;
        AbsentAttendees = settings.ConclusionItem.AbsentAttendees;
        RecurrenceName = settings.Recurrence.Name;
        Email = email;
        UsersName = name;
        UserId = uid;
        ShowDetails = settings.ShowDetails;
        MeetingNotes = settings.MeetingNotes;
        TimeZoneOffset = timezoneOffset;
        AverageMeetingRating = settings.Meeting.AverageMeetingRating;
        CompleteTime = settings.Meeting.CompleteTime;
        StartTime = settings.Meeting.StartTime;
        MeetingId = settings.Meeting.Id;
        HasAudio = settings.Meeting.HasRecording ?? false;
        TodoCompletion = settings.Meeting.TodoCompletion;
        ProductName = Config.ProductName(null);
        ClosedIssues = settings.ConclusionItem.ClosedIssues;
        OutstandingTodos = settings.ConclusionItem.OutstandingTodos;
        MeetingHeadlines = settings.ConclusionItem.MeetingHeadlines;
        PageSummaryNotes = settings.ConclusionItem.PageSummaryNotes;
        RecurrenceId = settings.Meeting.L10RecurrenceId;
        DateFormat = dateFormat;
        Now = settings.ExecutionTime;
        WhiteboardFileId = settings.ConclusionItem.WhiteboardFileId;
        Terms = terms;
        IsToV3 = isToV3 ?? false;
        MeetingStatus = settings.ConclusionItem.MeetingStatus;
        PadLookup = settings.PadLookup;
      }
      public MeetingStats MeetingStatus { get; set; }
      public List<MeetingNote> MeetingNotes { get; set; }
      public bool IsToV3 { get; set; }
      public List<Models.UserOrganizationModel> AbsentAttendees { get; set; }
      public List<L10Meeting.L10Meeting_Attendee> Attendees { get; set; }
      public List<IssueModel.IssueModel_Recurrence> ClosedIssues { get; set; }
      public List<TodoModel> OutstandingTodos { get; set; }
      public List<PeopleHeadline> MeetingHeadlines { get; set; }
      public List<PageSummaryNotes> PageSummaryNotes { get; set; }
      public bool ShouldShowDetails(string padId) {
        if (padId == null)
          return false;
        if (!ShowDetails)
          return false;
        if (PadLookup == null || PadLookup[padId] == null)
          return false;
        if (String.IsNullOrWhiteSpace(PadLookup[padId].Value))
          return false;
        return true;
      }
      public HtmlString GetDetails(string padId) {
        if (ShouldShowDetails(padId))
          return PadLookup[padId];
        return new HtmlString("");
      }
      public string DateFormat { get; set; }
      public string Email { get; set; }
      public string RecurrenceName { get; set; }
      public HtmlString GetDate(DateTime date) {
        return new HtmlString(TimeData.ConvertFromServerTime(date, TimeZoneOffset).ToString(DateFormat ?? "MM-dd-yyyy"));
      }
      public string GetTodoColor(DateTime dueDate) {
        return dueDate <= Now ? "#F22659;" : " #333333;";
      }

      public class ConclusionEmailSettings {
        public ConclusionEmailSettings(DateTime executionTime, bool showDetails, long? onlySendToUser, TermsCollection terms, L10Recurrence recurrence, L10Meeting meeting, ConclusionItems conclusionItem, List<MeetingNote> meetingNotes, DefaultDictionary<string, HtmlString> padLookup) {
          ExecutionTime = executionTime;
          ShowDetails = showDetails;
          OnlySendToUser = onlySendToUser;
          Recurrence = recurrence;
          Meeting = meeting;
          ConclusionItem = conclusionItem;
          MeetingNotes = meetingNotes;
          Terms = terms;
          PadLookup = padLookup;
        }


        public DateTime ExecutionTime { get; set; }
        public bool ShowDetails { get; set; }
        public long? OnlySendToUser { get; set; }
        public L10Recurrence Recurrence { get; set; }
        public L10Meeting Meeting { get; set; }
        public ConclusionItems ConclusionItem { get; set; }
        public DefaultDictionary<string, HtmlString> PadLookup { get; set; }
        public List<MeetingNote> MeetingNotes { get; set; }
        public TermsCollection Terms { get; set; }

      }

      public static List<MeetingSummaryEmailData> BuildMeetingSummaryEmailList(ConclusionEmailSettings settings) {
        var o = new List<MeetingSummaryEmailData>();

        //build meeting summary email for Attendees
        var terms = settings.Terms;
        foreach (var c in settings.ConclusionItem.SendEmailsToAttendees) {

          if (settings.OnlySendToUser != null && settings.OnlySendToUser.Value != c.UserId) {
            continue;
          }

          var dateFormat = c.User.NotNull(x => x.GetTimeSettings().DateFormat) ?? settings.Recurrence.Organization.GetTimeSettings().DateFormat;
          var email = c.User.NotNull(x => x.GetEmail());
          var name = c.User.NotNull(x => x.GetName());
          var uid = c.UserId;
          var tzOffset = c.User.NotNull(x => x.GetTimezoneOffset());
          var isToV3 = c.User.NotNull(x => x.User.NotNull(y => y.IsUsingV3));

          o.Add(new MeetingSummaryEmailData(settings, dateFormat, tzOffset, email, terms, name, uid, isToV3));
        }
        //Subscriber
        foreach (var e in settings.ConclusionItem.SendEmailsToSubscribers) {
          if (settings.OnlySendToUser != null) {
            break;
          }
          var dateFormat = settings.Recurrence.Organization.GetTimeSettings().DateFormat;
          var email = e.Who;
          var tzOffset = settings.Recurrence.Organization.GetTimezoneOffset();

          o.Add(new MeetingSummaryEmailData(settings, dateFormat, tzOffset, email, terms, null, null, false));
        }
        return o;
      }
    }

    [Queue(HangfireQueues.Immediate.CONCLUSION_EMAIL)]/*Queues must be lowecase alphanumeric. You must add queues to BackgroundJobServerOptions in Startup.auth.cs*/
    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public static async Task SendConclusionEmail_Unsafe(long meetingId, long? onlySendToUser, bool sendToExternal, [ActivateParameter] INotesProvider notesProvider, bool isUsingV3, List<string> meetingNotesIds = null) {

      var unsent = new List<Mail>();
      var meetingNotes = new List<MeetingNote>();
      long recurrenceId = 0;
      var error = "";
      bool showV3Features = VariableAccessor.Get(Variable.Names.V3_SHOW_FEATURES, () => false);

      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          try
          {
            var meeting = s.Get<L10Meeting>(meetingId);
            recurrenceId = meeting.L10RecurrenceId;
            var recurrence = s.Get<L10Recurrence>(recurrenceId);
            var notes = new List<L10Note>();

            if (showV3Features && meetingNotesIds != null && meetingNotesIds.Any())
              notes = L10Accessor.GetVisibleL10Notes_Unsafe(new List<long> { recurrenceId }).Where(note => meetingNotesIds.Contains(note.PadId)).ToList();

            if (!showV3Features)
              notes = L10Accessor.GetVisibleL10Notes_Unsafe(new List<long> { recurrenceId });

            if (notes.Any())
            {
              var notesHtml = (await notesProvider.GetHtmlForPads(notes.Select(x => x.PadId).ToArray())).ToDefaultDictionary(x => x.Key, x => x.Value, x => new HtmlString("")).ToArray();
              foreach ((L10Note x, int i) in notes.Select((x, i) => (x, i)))
              {
                if (x.DateLastModified != 0)
                {
                  meetingNotes.Add(new MeetingNote(x.Name, notesHtml.Where(h => h.Key == x.PadId).Select(h => h.Value).Single(), x.DateLastModified));
                }
                else
                {
                  meetingNotes.Add(new MeetingNote(x.Name, notesHtml.Where(h => h.Key == x.PadId).Select(h => h.Value).Single(), x.CreateTime.ToUnixTimeStamp()));
                }
              }
            }

            var terms = TermsAccessor.GetTermsCollection_Unsafe(s, recurrence.OrganizationId);

            //get meetting subscribers
            var conclusionItems = ConclusionItems.Get_Unsafe(s, meetingId, recurrenceId);
            var now = DateTime.UtcNow;

            var pads = conclusionItems.ClosedIssues.Select(x => x.Issue.PadId).ToList();
            pads.AddRange(conclusionItems.OutstandingTodos.Select(x => x.PadId));
            pads.AddRange(conclusionItems.MeetingHeadlines.Select(x => x.HeadlinePadId));
            pads.RemoveAll(item => item == null);
            var padTexts = (await notesProvider.GetHtmlForPads(pads)).ToDefaultDictionary(x => x.Key, x => x.Value, x => new HtmlString(""));

            var settings = new ConclusionEmailSettings(now, true, onlySendToUser, terms, recurrence, meeting, conclusionItems, meetingNotes, padTexts);
            var data = MeetingSummaryEmailData.BuildMeetingSummaryEmailList(settings);

            var emailType = showV3Features && isUsingV3 ? "V3.cshtml" : ".cshtml";

            foreach (var d in data)
            {
              var r = ViewUtility.RenderView("~/Views/Email/MeetingSummary" + emailType, d);

              var html = await r.ExecuteAsync();

              var mail = Mail.To(EmailTypes.L10Summary, d.Email)
                .Subject(EmailStrings.MeetingSummary_Subject, recurrence.Name)
                .Body(html);

              unsent.Add(mail);
            }


          }
          catch (Exception e)
          {
            log.Error("Emailer issue(1):" + recurrenceId, e);
            error += "(1)" + e.Message;
          }

          tx.Commit();
          s.Flush();
        }
      }

      try {
        if (unsent.Any()) {
          await Emailer.SendEmails(unsent, tableWidth: 595, showHeadImg: !showV3Features, showV3Features: showV3Features);
        }
      } catch (Exception e) {
        log.Error("Emailer issue(2):" + recurrenceId, e);
        error += "(2)" + e.Message;
      }

      if (!string.IsNullOrWhiteSpace(error)) {
        throw new Exception(error);
      }

    }


  }
}