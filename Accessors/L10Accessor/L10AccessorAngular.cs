using Humanizer;
using NHibernate;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.Models.L10;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Rocks;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.NHibernate;
//using System.Web.WebPages.Html;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
  public partial class L10Accessor : BaseAccessor {

    #region Angular

    public static async Task<AngularRecurrence> GetOrGenerateAngularRecurrence(UserOrganizationModel caller, long recurrenceId, bool includeScores = true, bool includeHistorical = true, bool fullScorecard = true, DateRange range = null, bool forceIncludeTodoCompletion = false, DateRange scorecardRange = null, bool populateManaging = false, bool includeCreatedBy = false, bool includeArchivedHeadlines = false) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var angular = await GetOrGenerateAngularRecurrence(s, perms, recurrenceId, includeScores, includeHistorical, fullScorecard, range, forceIncludeTodoCompletion, scorecardRange, includeCreatedBy, includeArchivedHeadlines: includeArchivedHeadlines);

          if (populateManaging) {
            var permissionLookup = new DefaultDictionary<long, bool>(id => perms.IsPermitted(x => x.CanAdminMeetingItemsForUser(id, recurrenceId)));
            foreach (var item in angular.Rocks) {
              item.Owner.Managing = permissionLookup[item.Owner.Id];
            }

            foreach (var item in angular.Attendees) {
              item.Managing = permissionLookup[item.Id];
            }
          }

          tx.Commit();
          s.Flush();

          return angular;
        }
      }
    }

    [Obsolete("Must call commit")]
    public static async Task<AngularRecurrence> GetOrGenerateAngularRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, bool includeScores = true, bool includeHistorical = true, bool fullScorecard = true, DateRange range = null, bool forceIncludeTodoCompletion = false, DateRange scorecardRange = null, bool includeCreatedBy = false, bool includeArchivedHeadlines = false) {
      perms.ViewL10Recurrence(recurrenceId);
      var recurrence = s.Get<L10Recurrence>(recurrenceId);
      _LoadRecurrences(s, LoadMeeting.True(), recurrence);

      var recur = new AngularRecurrence(recurrence);

      recur.Attendees = recurrence._DefaultAttendees.Select(x => {
        var au = AngularUser.CreateUser(x.User);
        au.CreateTime = x.CreateTime;
        return au;
      }).ToList();

      scorecardRange = scorecardRange ?? range;
      bool includeClosedHeadlines = true;
      DateRange lookupRange = null;
      if (range != null) {
        lookupRange = new DateRange(range.StartTime, range.EndTime);
      } else {
        includeClosedHeadlines = false;
      }

      if (fullScorecard) {
        var period = perms.GetCaller().GetTimeSettings().Period;

        switch (period) {
          case ScorecardPeriod.Monthly:
            scorecardRange = new DateRange(DateTime.UtcNow.AddMonths(-12).StartOfWeek(DayOfWeek.Sunday), DateTime.UtcNow.AddMonths(1).StartOfWeek(DayOfWeek.Sunday));
            lookupRange = new DateRange(DateTime.UtcNow.AddMonths(-12).AddDays(-37).StartOfWeek(DayOfWeek.Sunday), DateTime.UtcNow.AddMonths(1).StartOfWeek(DayOfWeek.Sunday));
            break;
          case ScorecardPeriod.Quarterly:
            scorecardRange = new DateRange(DateTime.UtcNow.AddMonths(-36).StartOfWeek(DayOfWeek.Sunday), DateTime.UtcNow.AddMonths(1).StartOfWeek(DayOfWeek.Sunday));
            lookupRange = new DateRange(DateTime.UtcNow.AddMonths(-36).AddDays(-37).AddMonths(-37).StartOfWeek(DayOfWeek.Sunday), DateTime.UtcNow.AddMonths(1).StartOfWeek(DayOfWeek.Sunday));
            break;
          default:
            scorecardRange = new DateRange(DateTime.UtcNow.AddDays(-7 * TimingUtility.STANDARD_SCORECARD_WEEKS).StartOfWeek(DayOfWeek.Sunday), DateTime.UtcNow.AddDays(9).StartOfWeek(DayOfWeek.Sunday));
            lookupRange = new DateRange(DateTime.UtcNow.AddDays(-7 * (TimingUtility.STANDARD_SCORECARD_WEEKS+1)).StartOfWeek(DayOfWeek.Sunday), DateTime.UtcNow.AddDays(9).StartOfWeek(DayOfWeek.Sunday));
            break;
        }
      }
      var scores = new List<ScoreModel>();

      var scoresAndMeasurables = await GetOrGenerateScorecardDataForRecurrence(s, perms, recurrenceId, true, range: lookupRange, getMeasurables: true, getScores: includeScores, forceIncludeTodoCompletion: forceIncludeTodoCompletion, queryCache: recurrence._CacheQueries);

      if (includeScores) {
        scores = scoresAndMeasurables.Scores;
      }

      if (includeCreatedBy) {
        recur.CreatedBy = AngularUser.CreateUser(UserAccessor.GetUserOrganization(s, perms, recurrence.CreatedById, false, false));
      }

      var measurables = scoresAndMeasurables.MeasurablesAndDividers.Select(x => {
        if (x.IsDivider) {
          var m = AngularMeasurable.CreateDivider(x);
          m.RecurrenceId = x.L10Recurrence.Id;
          return m;
        } else {
          var m = new AngularMeasurable(x.Measurable, false);
          m.Ordering = x._Ordering;
          m.RecurrenceId = x.L10Recurrence.Id;
          return m;
        }
      }).ToList();

      if (recurrence.IncludeAggregateTodoCompletion || forceIncludeTodoCompletion) {
        measurables.Add(new AngularMeasurable(TodoMeasurable) {
          Ordering = -2
        });
      }

      var ts = perms.GetCaller().GetTimeSettings();
      ts.WeekStart = recurrence.StartOfWeekOverride ?? ts.WeekStart;
      recur.Scorecard = new AngularScorecard(recurrenceId, ts, measurables, scores, DateTime.UtcNow, scorecardRange, reverseScorecard: recurrence.ReverseScorecard);

      var allRocks = recurrence._DefaultRocks.Select(x => new AngularRock(x)).ToList();

      if (range != null) {
        RockModel rockAlias = null;
        var histRock = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
          .Where(x => x.DeleteTime != null && x.L10Recurrence.Id == recurrenceId)
          .Where(range.Filter<L10Recurrence.L10Recurrence_Rocks>())
          .List();

        allRocks.AddRange(histRock.Select(x => new AngularRock(x)));
      }
      recur.Rocks = allRocks.Distinct(x => x.Id);
      recur.Todos = GetAllTodosForRecurrence(s, perms, recurrenceId, includeClosed: includeHistorical, range: range).Select(x => new AngularTodo(x)).OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ToList();
      recur.IssuesList.Issues = GetAllIssuesForRecurrence(s, perms, recurrenceId, includeCompleted: includeHistorical, range: range).Select(x => new AngularIssue(x)).OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ToList();
      recur.Headlines = GetAllHeadlinesForRecurrence(s, perms, recurrenceId, includeClosed: includeClosedHeadlines, range: range, includeArchivedHeadlines: includeArchivedHeadlines).Select(x => new AngularHeadline(x)).OrderByDescending(x => x.CloseTime ?? DateTime.MaxValue).ToList();
      recur.Notes = recurrence._MeetingNotes.Select(x => new AngularMeetingNotes(x)).ToList();

      recur.ShowSegue = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Segue);
      recur.ShowScorecard = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Scorecard);
      recur.ShowRockReview = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Rocks);
      recur.ShowHeadlines = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Headlines);
      recur.ShowTodos = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Todo);
      recur.ShowIDS = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.IDS);
      recur.ShowConclude = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Conclude);

      recur.SegueMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.Segue).NotNull(x => (decimal?)x.Minutes);
      recur.ScorecardMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.Scorecard).NotNull(x => (decimal?)x.Minutes);
      recur.RockReviewMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.Rocks).NotNull(x => (decimal?)x.Minutes);
      recur.HeadlinesMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.Headlines).NotNull(x => (decimal?)x.Minutes);
      recur.TodosMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.Todo).NotNull(x => (decimal?)x.Minutes);
      recur.IDSMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.IDS).NotNull(x => (decimal?)x.Minutes);
      recur.ConcludeMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.Conclude).NotNull(x => (decimal?)x.Minutes);

      recur.MeetingType = recurrence.MeetingType;


      if (range == null) {
        recur.date = new AngularDateRange() {
          startDate = DateTime.UtcNow.Date.AddDays(-9),
          endDate = DateTime.UtcNow.Date.AddDays(1),
        };
      } else {
        recur.date = new AngularDateRange() {
          startDate = range.StartTime,
          endDate = range.EndTime,
        };
      }

      recur.HeadlinesUrl = Config.NotesUrl("p/" + recurrence.HeadlinesId + "?showControls=true&showChat=false");
      return recur;
    }

    public static async Task Remove(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, UserOrganizationModel caller, BaseAngular model, long recurrenceId, string connectionId = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await using (var rt = RealTimeUtility.Create(connectionId)) {
            var detachTime = DateTime.UtcNow;
            var perms = PermissionsUtility.Create(s, caller);
            perms.EditL10Recurrence(recurrenceId);

            if (model.Type == typeof(AngularIssue).Name) {
              await CompleteIssue(s, perms, model.Id);
            } else if (model.Type == typeof(AngularTodo).Name) {
              await TodoAccessor.CompleteTodo(s, perms, model.Id, detachTime);
            } else if (model.Type == typeof(AngularRock).Name) {
              await RemoveRock(s, perms, rt, recurrenceId, model.Id, detachTime: detachTime);
            } else if (model.Type == typeof(AngularMeasurable).Name) {
              await DetachMeasurable(dbContext, s, perms, rt, recurrenceId, model.Id, true, detachTime);
            } else if (model.Type == typeof(AngularUser).Name) {
              await RemoveAttendee(s, perms, rt, recurrenceId, model.Id, detachTime: detachTime);
            } else if (model.Type == typeof(AngularHeadline).Name) {
              await RemoveHeadline(s, perms, rt, model.Id, closeTime: detachTime);
            } else {
              throw new PermissionsException("Unhandled type: " + model.Type);
            }

            tx.Commit();
            s.Flush();
          }
        }
      }
    }

    public static async Task UnArchive(UserOrganizationModel caller, BaseAngular model, long recurrenceId, string connectionId = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await using (var rt = RealTimeUtility.Create(connectionId)) {
            var perms = PermissionsUtility.Create(s, caller);
            perms.EditL10Recurrence(recurrenceId);


            if (model.Type == typeof(AngularIssue).Name) {
              await UnarchiveIssue(OrderedSession.Indifferent(s), perms, rt, model.Id);
            } else if (model.Type == typeof(AngularRock).Name) {
              await UnarchiveRock(s, perms, rt, recurrenceId, model.Id);
            } else if (model.Type == typeof(AngularHeadline).Name) {
              await UnarchiveHeadline(s, perms, rt, model.Id);
            } else {
              throw new PermissionsException("Unhandled type: " + model.Type);
            }

            tx.Commit();
            s.Flush();
          }
        }
      }
    }


    public static async Task UnarchiveIssue(IOrderedSession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceIssue) {
      var issue = s.Get<IssueModel.IssueModel_Recurrence>(recurrenceIssue);
      perm.EditL10Recurrence(issue.Recurrence.Id);
      if (issue.CloseTime == null) {
        throw new PermissionsException("Issue already unarchived.");
      }

      var builder = IssuesAccessor.BuildEditIssueExecutor(recurrenceIssue, complete:false);
      await SyncUtil.ExecuteNonAtomically(s, perm, builder);
      //await IssuesAccessor.EditIssue(s, perm, recurrenceIssue, complete: false);
    }

    public static async Task UnarchiveHeadline(ISession s, PermissionsUtility perm, RealTimeUtility rt, long headlineId) {
      perm.ViewHeadline(headlineId);

      var r = s.Get<PeopleHeadline>(headlineId);

      if (r.CloseTime == null) {
        throw new PermissionsException("Headline already unarchived.");
      }

      perm.EditL10Recurrence(r.RecurrenceId);

      var now = DateTime.UtcNow;
      r.CloseTime = null;
      s.Update(r);

      await HooksRegistry.Each<IHeadlineHook>((ses, x) => x.UnArchiveHeadline(ses, r));
    }

    //[Untested("Vto_Rocks",/* "Is the rock correctly removed in real-time from L10",/* "Is the goal correctly removed in real-time from VTO",*/ "Is goal correctly archived when existing in no meetings?")]
    public static async Task UnarchiveRock(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceId, long rockId) {
      //perm.AdminL10Recurrence(recurrenceId).EditRock(rockId);
      perm.AdminL10Recurrence(recurrenceId).EditRock_UnArchive(rockId);

      await RockAccessor.UnArchiveRock(s, perm, rockId);

      // attach goal
      await AttachRock(s, perm, recurrenceId, rockId, false, AttachRockType.Unarchive);

    }


    public static async Task Update(UserOrganizationModel caller, BaseAngular model, string connectionId) {
      if (model.Type == typeof(AngularIssue).Name) {
        var m = (AngularIssue)model;
        //UpdateIssue(caller, (long)model.GetOrDefault("Id", null), (string)model.GetOrDefault("Name", null), (string)model.GetOrDefault("Details", null), (bool?)model.GetOrDefault("Complete", null), connectionId);
        await IssuesAccessor.EditIssue(caller, m.Id, m.Name ?? "", m.Complete, priority: m.Priority, owner: m.Owner.NotNull(x => (long?)x.Id));
      } else if (model.Type == typeof(AngularTodo).Name) {
        var m = (AngularTodo)model;
        if (m.TodoType == TodoType.Milestone) {
          await RockAccessor.EditMilestone(caller, -m.Id, m.Name, m.DueDate, status: m.Complete == true ? MilestoneStatus.Done : MilestoneStatus.NotDone, connectionId: connectionId);
        } else {
          var completeTime = DateTime.UtcNow;
          await TodoAccessor.UpdateTodo(caller, m.Id, completeTime: completeTime, m.Name, m.DueDate, m.Owner.NotNull(x => (long?)x.Id), m.CloseTime, m.Complete);// null, m.DueDate, m.Owner.NotNull(x => (long?)x.Id), m.Complete, connectionId);
        }
      } else if (model.Type == typeof(AngularScore).Name) {
        var m = (AngularScore)model;
        var measurableId = m.Measurable.NotNull(x => x.Id);
        measurableId = measurableId == 0 ? m.MeasurableId : measurableId;
        await ScorecardAccessor.UpdateScore(caller, m.Id, measurableId, TimingUtility.GetDateSinceEpoch(m.ForWeek), m.Measured);
      } else if (model.Type == typeof(AngularMeetingNotes).Name) {
        var m = (AngularMeetingNotes)model;
        await EditNote(caller, m.Id,/* m.Contents,*/ m.Title, connectionId);
      } else if (model.Type == typeof(AngularRock).Name) {
        var m = (AngularRock)model;
        //TODO re-add company goal
        await UpdateRock(caller, m.Id, m.Name, m.Completion, m.Owner.NotNull(x => (long?)x.Id), connectionId, dueDate: m.DueDate, recurrenceRockId: m.RecurrenceRockId, vtoRock: m.VtoRock);
      } else if (model.Type == typeof(AngularBasics).Name) {
        var m = (AngularBasics)model;
        await UpdateRecurrence(caller, m.Id, m.Name, m.TeamType, connectionId);
      } else if (model.Type == typeof(AngularHeadline).Name) {
        var m = (AngularHeadline)model;
        await HeadlineAccessor.UpdateHeadline(caller, m.Id, m.Name);
      } else {
        throw new PermissionsException("Unhandled type: " + model.Type);
      }
    }

    public static async Task UpdateRecurrence(UserOrganizationModel caller, long recurrenceId, DateTime? tangentAlertTimestamp)
    {
      await using (var rt = RealTimeUtility.Create())
      {
        using (var s = HibernateSession.GetCurrentSession())
        {
          using (var tx = s.BeginTransaction())
          {
            var userperms = PermissionsUtility.Create(s, caller);
            var permittedToSetTangentAlert =
                userperms.IsPermitted(x => x.ViewL10Recurrence(recurrenceId)) ||
                userperms.IsPermitted(x => x.EditL10Recurrence(recurrenceId)) ||
                userperms.IsPermitted(x => x.AdminL10Recurrence(recurrenceId));

            var recurrence = s.Get<L10Recurrence>(recurrenceId);

            if (permittedToSetTangentAlert && recurrence.DeleteTime == null)
            {
              var angular = new AngularBasics(recurrenceId);

              recurrence.TangentAlertTimestamp = tangentAlertTimestamp;
              s.Update(recurrence);
              rt.UpdateRecurrences(recurrenceId).Update(angular);
              await HooksRegistry.Each<ITangentHook>((ses, x) => x.ShowTangent(ses, caller, recurrenceId));

              tx.Commit();
              s.Flush();
            }
            else
            {
              throw new PermissionsException();
            }
          }
        }
      }
    }

    public static async Task UpdateMeetingLastViewedTimestamp(UserOrganizationModel caller, MeetingEditLastViewedTimestampModel meetingEditLastViewed)
    {
      await using (var rt = RealTimeUtility.Create())
      {
        using (var s = HibernateSession.GetCurrentSession())
        {
          using (var tx = s.BeginTransaction())
          {
            var perms = PermissionsUtility.Create(s, caller);
            perms.ViewL10Recurrence(meetingEditLastViewed.MeetingId);

            var angular = new AngularBasics(meetingEditLastViewed.MeetingId);
            var recurrence = s.Get<L10Recurrence>(meetingEditLastViewed.MeetingId);
            if (recurrence.DeleteTime != null)
            {
              throw new PermissionsException();
            }
            var dateUpdated = meetingEditLastViewed.LastViewedTimestamp.FromUnixTimeStamp();
            recurrence.LastViewedTimestamp = meetingEditLastViewed.LastViewedTimestamp;
            s.Update(recurrence);
            rt.UpdateRecurrences(recurrence.Id).Update(angular);

            await HooksRegistry.Each<IMeetingEvents>((sess, x) => x.UpdateRecurrence(sess, caller, recurrence));

            s.Flush();
            tx.Commit();
          }
        }
      }

        }
    public static async Task UpdateRecurrenceConcludeActions(UserOrganizationModel caller, long recurrenceId, ConcludeActionsModel concludeActionsModel = null)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {

          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewL10Recurrence(recurrenceId);
          var recurrence = s.Get<L10Recurrence>(recurrenceId);
          if (recurrence.DeleteTime != null)
          {
            throw new PermissionsException();
          }

          if (concludeActionsModel != null)
          {
            if (recurrence.MeetingConclusion != null)
              UpdateConcludeMeetingActions(recurrenceId, concludeActionsModel, s, recurrence);
            else
              SaveConcludeMeetingActions(recurrenceId, concludeActionsModel, s, recurrence);

            if (concludeActionsModel.DisplayMeetingRatings.HasValue)
            {
              await HooksRegistry.Each<IMeetingRatingHook>((ses, x) => x.ShowHiddeRating(caller, recurrence, concludeActionsModel.DisplayMeetingRatings ?? false));
            }
          }

          await HooksRegistry.Each<IMeetingEvents>((sess, x) => x.UpdateRecurrence(sess, caller, recurrence));

          s.Update(recurrence);
          tx.Commit();

        }
      }
    }

    public static async Task UpdateRecurrence(UserOrganizationModel caller, long recurrenceId, DateTime? tangentAlertTimestamp, string name = null, L10TeamType? teamType = null, string connectionId = null, ConcludeActionsModel concludeActionsModel = null, string videoConferenceLink = null, MeetingEditModel meetingEditModel = null, bool? showNumberedIssueList = null, bool ignoreTangentAlertTimestamp = false)
    {
      await UpdateRecurrence(caller, recurrenceId, ignoreTangentAlertTimestamp, tangentAlertTimestamp, name, teamType, connectionId, concludeActionsModel, videoConferenceLink: videoConferenceLink, meetingEditModel, showNumberedIssueList);
    }

    public static async Task UpdateRecurrence(UserOrganizationModel caller, long recurrenceId, string name = null, L10TeamType? teamType = null, string connectionId = null, ConcludeActionsModel concludeActionsModel = null) {
      await UpdateRecurrence(caller, recurrenceId, ignoreTangentAlertTimestamp: true, tangentAlertTimestamp: null, name, teamType, connectionId, concludeActionsModel);
    }

    private static async Task UpdateRecurrence(UserOrganizationModel caller, long recurrenceId, bool ignoreTangentAlertTimestamp, DateTime? tangentAlertTimestamp, string name, L10TeamType? teamType, string connectionId, ConcludeActionsModel concludeActionsModel = null, string videoConferenceLink = null, MeetingEditModel meetingEditModel = null, bool? showNumberedIssueList = null)
    {
      await using (var rt = RealTimeUtility.Create(connectionId)) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {

            var perms = PermissionsUtility.Create(s, caller);

            if(meetingEditModel == null)
            {
              verifyEditViewRecurrencePermissions(perms, recurrenceId);
            }
            else
            {
              perms.EditL10Recurrence(recurrenceId);
            }

            var recurrence = s.Get<L10Recurrence>(recurrenceId);

            if (recurrence.DeleteTime != null) {
              throw new PermissionsException();
            }

            var angular = new AngularBasics(recurrenceId);

            name = name?.Replace("&amp;", "&");
            if (name != null && recurrence.Name != name) {
              recurrence.Name = name;
              angular.Name = name;
              await Depristine_Unsafe(s, caller, recurrence);
            }

            if (teamType != null && recurrence.TeamType != teamType) {
              recurrence.TeamType = teamType.Value;
              angular.TeamType = teamType;
              await Depristine_Unsafe(s, caller, recurrence);
            }

            if(videoConferenceLink != null)
            {
              recurrence.VideoConferenceLink = videoConferenceLink;
            }

            if(showNumberedIssueList != null)
            {
              recurrence.ShowNumberedIssueList = (bool)showNumberedIssueList;
            }

            if (meetingEditModel != null)
            {
              var issueVoting = meetingEditModel.IssueVoting;

              if (meetingEditModel.ScheduledStartTime.HasValue)
                recurrence.L10MeetingInProgress.StartTime = meetingEditModel.ScheduledStartTime.FromUnixTimeStamp();

              if (meetingEditModel.ScheduledEndTime.HasValue)
                recurrence.L10MeetingInProgress.CompleteTime = meetingEditModel.ScheduledEndTime.FromUnixTimeStamp();

              if (meetingEditModel.LastViewedTimestamp.HasValue)
                recurrence.LastViewedTimestamp = meetingEditModel.LastViewedTimestamp;

              if (meetingEditModel.ReverseMetrics.HasValue)
                recurrence.ReverseScorecard = meetingEditModel.ReverseMetrics.Value;

              if (meetingEditModel.BusinessPlanId != null)
                recurrence.BusinessPlanId = meetingEditModel.BusinessPlanId;
              

        //      HighlightPreviousWeekForMetrics = source.CurrentWeekHighlightShift == -1,
        //ReverseMetrics = source.ReverseScorecard,
        //PreventEditingUnownedMetrics = source.PreventEditingUnownedMeasurables,



              // Per Frontend
              // Recurrence.Prioritization: Rank = 'PRIORITY' in v3.
              // Recurrence.Prioritization: Priority = 'STAR' in v3.
              if (!String.IsNullOrEmpty(issueVoting))
              {
                switch (issueVoting.ToUpper())
                {
                  case "STAR" :
                    recurrence.Prioritization = PrioritizationType.Priority;
                    break;
                  case "PRIORITY":
                    recurrence.Prioritization = PrioritizationType.Rank;
                    break;

                  default :
                    break;
                }
              }
            }

            if(!ignoreTangentAlertTimestamp)
              recurrence.TangentAlertTimestamp = tangentAlertTimestamp;

            if (concludeActionsModel != null)
            {
              if (recurrence.MeetingConclusion != null)
                UpdateConcludeMeetingActions(recurrenceId, concludeActionsModel, s, recurrence);
              else
                SaveConcludeMeetingActions(recurrenceId, concludeActionsModel, s, recurrence);

              if(concludeActionsModel.DisplayMeetingRatings.HasValue)
              {
                await HooksRegistry.Each<IMeetingRatingHook>((ses, x) => x.ShowHiddeRating(caller, recurrence, concludeActionsModel.DisplayMeetingRatings ?? false));
              }
            }

            s.Update(recurrence);
            rt.UpdateRecurrences(recurrenceId).Update(angular);

            if(!ignoreTangentAlertTimestamp)
              await HooksRegistry.Each<ITangentHook>((ses, x) => x.ShowTangent(ses, caller, recurrenceId));

            await HooksRegistry.Each<IMeetingEvents>((sess, x) => x.UpdateRecurrence(sess, caller, recurrence));

            tx.Commit();
            s.Flush();
          }
        }
      }
    }

    private static void SaveConcludeMeetingActions(long recurrenceId, ConcludeActionsModel concludeActionsModel, ISession session, L10Recurrence recurrence)
    {
      SendEmailSummaryTo sendEmailSummaryTo = SendEmailSummaryTo.NONE;
      FeedbackStyle feedbackStyle = FeedbackStyle.ALL_PARTICIPANTS;

      if (!String.IsNullOrEmpty(concludeActionsModel.SendEmailSummaryTo))
        sendEmailSummaryTo = Enum.Parse<SendEmailSummaryTo>(concludeActionsModel.SendEmailSummaryTo);

      if (!String.IsNullOrEmpty(concludeActionsModel.FeedbackStyle))
        feedbackStyle = Enum.Parse<FeedbackStyle>(concludeActionsModel.FeedbackStyle);

      var actionsConclude = new MeetingConclusionModel()
      {
        SendEmailSummaryTo = sendEmailSummaryTo,
        IncludeMeetingNotesInEmailSummary = concludeActionsModel.IncludeMeetingNotesInEmailSummary ?? false,
        ArchiveCompletedTodos = concludeActionsModel.ArchiveCompletedTodos ?? false,
        ArchiveHeadlines = concludeActionsModel.ArchiveHeadlines ?? false,
        ArchiveCompletedIssues = concludeActionsModel.ArchiveCompletedIssues ?? false,
        DisplayMeetingRatings = concludeActionsModel.DisplayMeetingRatings ?? false,
        FeedbackStyle = feedbackStyle,
        L10RecurrenceId = recurrenceId,
        DateLastModified = DateTime.UtcNow.ToUnixTimeStamp()
      };
      recurrence.MeetingConclusion = actionsConclude;
      if (concludeActionsModel.DisplayMeetingRatings.HasValue)
        recurrence.DisplayMeetingRatings = concludeActionsModel.DisplayMeetingRatings ?? false;

      session.Update(recurrence);
    }
    private static void UpdateConcludeMeetingActions(long recurrenceId, ConcludeActionsModel concludeActionsModel, ISession session, L10Recurrence recurrence)
    {
      SendEmailSummaryTo? sendEmailSummaryTo = null;
      FeedbackStyle? feedbackStyle = null;
      if (!String.IsNullOrEmpty(concludeActionsModel.SendEmailSummaryTo))
        sendEmailSummaryTo = Enum.Parse<SendEmailSummaryTo>(concludeActionsModel.SendEmailSummaryTo);

      if (!String.IsNullOrEmpty(concludeActionsModel.FeedbackStyle))
        feedbackStyle = Enum.Parse<FeedbackStyle>(concludeActionsModel.FeedbackStyle);

      var concludeActions = session.Get<MeetingConclusionModel>(recurrence.MeetingConclusion.Id);
      if (concludeActions.DeleteTime == null)
      {
        //ArchiveCompletedIssues
        if (concludeActionsModel.ArchiveCompletedIssues.HasValue && concludeActions.ArchiveCompletedIssues != concludeActionsModel.ArchiveCompletedIssues)
          concludeActions.ArchiveCompletedIssues = concludeActionsModel.ArchiveCompletedIssues;
        //ArchiveCompletedTodos
        if (concludeActionsModel.ArchiveCompletedTodos.HasValue && concludeActions.ArchiveCompletedTodos != concludeActionsModel.ArchiveCompletedTodos)
          concludeActions.ArchiveCompletedTodos = concludeActionsModel.ArchiveCompletedTodos;
        //ArchiveHeadlines
        if (concludeActionsModel.ArchiveHeadlines.HasValue && concludeActions.ArchiveHeadlines != concludeActionsModel.ArchiveHeadlines)
          concludeActions.ArchiveHeadlines = concludeActionsModel.ArchiveHeadlines;
        //DisplayMeetingRatings
        if (concludeActionsModel.DisplayMeetingRatings.HasValue && concludeActions.DisplayMeetingRatings != concludeActionsModel.DisplayMeetingRatings)
          concludeActions.DisplayMeetingRatings = concludeActionsModel.DisplayMeetingRatings;
        //IncludeMeetingNotesInEmailSummary
        if (concludeActionsModel.IncludeMeetingNotesInEmailSummary.HasValue && concludeActions.IncludeMeetingNotesInEmailSummary != concludeActionsModel.IncludeMeetingNotesInEmailSummary)
          concludeActions.IncludeMeetingNotesInEmailSummary = concludeActionsModel.IncludeMeetingNotesInEmailSummary;
        //FeedbackStyle
        if (feedbackStyle != null && concludeActions.FeedbackStyle != feedbackStyle)
          concludeActions.FeedbackStyle = feedbackStyle;
        //SendEmailSummaryTo
        if (sendEmailSummaryTo != null && concludeActions.SendEmailSummaryTo != sendEmailSummaryTo)
          concludeActions.SendEmailSummaryTo = sendEmailSummaryTo;

        if (concludeActionsModel.DisplayMeetingRatings.HasValue)
          recurrence.DisplayMeetingRatings = concludeActionsModel.DisplayMeetingRatings ?? false;

        concludeActions.DateLastModified = DateTime.UtcNow.ToUnixTimeStamp();
        session.Update(concludeActions);
      }
    }

    #endregion
  }
}
