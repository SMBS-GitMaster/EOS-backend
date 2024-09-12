using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RadialReview.Models.L10;
using RadialReview.Models.Rocks;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Users;
using FluentNHibernate.Conventions;
using L10PageType = RadialReview.Models.L10.L10Recurrence.L10PageType;
using RadialReview.Models.Application;
using RadialReview.Models.L10.VM;
using RadialReview.Core.Models.Scorecard;
using RadialReview.Core.GraphQL.Models.Mutations;
using Humanizer;
using RadialReview.Models.Enums;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Models.Query;
using RadialReview.Utilities.DataTypes;
using RadialReview.Core.GraphQL.Enumerations;
using ModelIssue = RadialReview.Models.Issues.IssueModel;
using RadialReview.Models;
using RadialReview.Core.GraphQL.MeetingListLookup;
using RadialReview.Core.GraphQL.MetricFormulaLookup;
using RadialReview.Models.Scorecard;
using RadialReview.Core.GraphQL.MetricAddExistingLookup;
using static RadialReview.Accessors.L10Accessor;
using RadialReview.Models.Dashboard;
using RadialReview.Core.GraphQL.Common;

namespace RadialReview.Core.Repositories
{

  /// <summary>
  /// This file is not meant to go to the database, it just transforms database models into GraphQL models
  /// I created this class so that it is easy to see which methods require permissions and which do not.
  /// This is because no permissions are required to simply transform a model.
  /// You should have checked permissions before you extracted the model from the database.
  /// </summary>
  public static class RepositoryTransformers
  {

    public static IdNamePairQueryModel RecurrenceIdNamePair(L10Recurrence source)
    {
      return new IdNamePairQueryModel
      {
        Id = source.Id,
        Name = source.Name
      };

    }

    public static MeetingQueryModel MeetingFromRecurrence(this L10Recurrence source, RadialReview.Models.UserOrganizationModel caller, RadialReview.Models.FavoriteModel favorite, RadialReview.Models.MeetingSettingsModel settings, long? userOrgId = null)
    {
      MeetingQueryModel result = new MeetingQueryModel
      {
        Id = source.Id,
        Name = source.Name,
        OrgId = source.OrganizationId,
        UserOrgId = userOrgId,
        OrgName = source.Organization.Name,
        Archived = source.DeleteTime != null,
        FavoriteId = favorite?.Id,
        FavoritedSortingPosition = favorite?.Position,
        FavoritedTimestamp = favorite?.CreatedDateTime.ToUnixTimeStamp(),
        UserId = caller.Id,
        Email = caller.GetEmail(),
        LastViewedTimestamp = source.LastViewedTimestamp,
        ScheduledStartTime = source.L10MeetingInProgress != null ? DateTime.UtcNow.ToUnixTimeStamp() /*source.L10MeetingInProgress.StartTime.ToUnixTimeStamp()*/ : null,
        ScheduledEndTime = source.L10MeetingInProgress != null ? DateTime.UtcNow.AddHours(1.5).ToUnixTimeStamp() /*source.L10MeetingInProgress.CompleteTime.ToUnixTimeStamp() */: null,
        MeetingType = source.MeetingType.ToString(),
        DateCreated = source.CreateTime.ToUnixTimeStamp(),
        CreatedTimestamp = source.CreateTime.ToUnixTimeStamp(),
        VideoConferenceLink = source.VideoConferenceLink, // New V3 field, not backwards compatible
        IssueVoting = ((gqlIssueVoting)source.Prioritization).ToString(), // Issue Voting is NOT a new field, it will be Prioritization but the enum names are changed.
        Version = source.Version,
        LastUpdatedBy = source.LastUpdatedBy,
        DateLastModified = source.DateLastModified,
        CurrentMeetingInstanceId = source.MeetingInProgress,
        StartOfWeekOverride = source.StartOfWeekOverride?.ToString(),
        ReverseMetrics = source.ReverseScorecard,
        ConcludeActions = source.TransformConcludeActions(),
        HighlightPreviousWeekForMetrics = source.CurrentWeekHighlightShift == -1,
        PreventEditingUnownedMetrics = source.PreventEditingUnownedMeasurables,
        BusinessPlanId = source.BusinessPlanId,
      };

      if (source._DefaultAttendees != null)
      {
        result.UserIsAttendee = source._DefaultAttendees.Any(a => a.User.Id == caller.Id);
        result.Attendees = source._DefaultAttendees.Select(x => MeetingAttendeeTransformer.TransformAttendee(x, false, false)).ToList().AsQueryable();
      }
      else
      {
        result.Attendees = new List<MeetingAttendeeQueryModel>().AsQueryable();
      }

      if (source._Pages != null)
      {
        result.ExpectedMeetingDurationFromAgendaInMinutes = source._Pages.Any() ? source._Pages.Sum(x => (decimal)x.Minutes) : 0.0m;
        result.MeetingPages = source._Pages.Select(page => RepositoryTransformers.MeetingPageFromL10RecurrencePage(page)).AsQueryable();
      }
      else
      {
        result.MeetingPages = new List<MeetingPageQueryModel>().AsQueryable();
      }

      return result;
    }

    public static MeetingListLookupModel TransformMeetingListLookup(this TinyRecurrence rec, long userId, RadialReview.Models.FavoriteModel favorite, RadialReview.Models.MeetingSettingsModel settings)
    {
      MeetingListLookupModel result = new MeetingListLookupModel
      {
        Id = rec.Id,
        Name = rec.Name,
        UserId = userId,
        DateCreated = rec.CreateTime.ToUnixTimeStamp(),
        CreatedTimestamp = rec.CreateTime.ToUnixTimeStamp(),
        LastViewedTimestamp = settings?.LastViewedTimestamp,
        UserIsAttendee = rec.IsAttendee,
        MeetingType = rec.MeetingType.ToString(),
        FavoriteId = favorite?.Id,
        FavoritedSortingPosition = favorite?.Position,
        FavoritedTimestamp = favorite?.CreatedDateTime.ToUnixTimeStamp(),
        Archived = rec.DeleteTime != null,
        LastUpdatedBy = rec.LastUpdatedBy,
        DateLastModified = rec.DateLastModified,
      };
      if (rec._DefaultAttendees != null)
      {
        result.AttendeesLookup = rec._DefaultAttendees
          .Select(x => x.TransformTinyUserToMeetingAttendee()).ToList().AsQueryable();
      }
      else
      {
        result.AttendeesLookup = new List<MeetingAttendeeQueryModelLookup>().AsQueryable();
      }
      return result;
    }

    public static MeetingListLookupModel TransformMeetingListLookupFromRecurrence(this L10Recurrence l10Recurrence, long userId, RadialReview.Models.FavoriteModel favorite, RadialReview.Models.MeetingSettingsModel settings, Boolean useAttendeesLookup = false)
    {
      MeetingListLookupModel result = new MeetingListLookupModel
      {
        Id = l10Recurrence.Id,
        Name = l10Recurrence.Name,
        UserId = userId,
        DateCreated = l10Recurrence.CreateTime.ToUnixTimeStamp(),
        CreatedTimestamp = l10Recurrence.CreateTime.ToUnixTimeStamp(),
        LastViewedTimestamp = settings.LastViewedTimestamp,
        MeetingType = l10Recurrence.MeetingType.ToString(),
        FavoriteId = favorite?.Id,
        FavoritedSortingPosition = favorite?.Position,
        FavoritedTimestamp = favorite?.CreatedDateTime.ToUnixTimeStamp(),
        Archived = l10Recurrence.DeleteTime != null,
        LastUpdatedBy = l10Recurrence.LastUpdatedBy,
        DateLastModified = l10Recurrence.DateLastModified,
        UserIsAttendee = l10Recurrence._DefaultAttendees?.Any(y => y.User.Id == userId) ?? false
      };

      if (useAttendeesLookup && l10Recurrence._DefaultAttendees != null)
      {
        result.AttendeesLookup = l10Recurrence._DefaultAttendees
         .Select(x => MeetingAttendeeTransformer.TransformAttendeeLookup(x)).ToList().AsQueryable();
      }
      else
      {
        result.AttendeesLookup = new List<MeetingAttendeeQueryModelLookup>().AsQueryable();
      }

      return result;
    }

    public static MeetingConcludeActionsModel TransformConcludeActions(this L10Recurrence source)
    {
      try
      {
        var conclusionActions = source.MeetingConclusion;
        if (conclusionActions is null)
          return MeetingConcludeActionsModel.Default();

        return new MeetingConcludeActionsModel
        {
          SendEmailSummaryTo = conclusionActions.SendEmailSummaryTo is null ? gqlSendEmailSummaryTo.NONE : EnumHelper.ConvertToNonNullable<gqlSendEmailSummaryTo>(conclusionActions.SendEmailSummaryTo),
          IncludeMeetingNotesInEmailSummary = conclusionActions.IncludeMeetingNotesInEmailSummary ?? false,
          ArchiveCompletedTodos = conclusionActions.ArchiveCompletedTodos ?? false,
          ArchiveHeadlines = conclusionActions.ArchiveHeadlines ?? false,
          DisplayMeetingRatings = conclusionActions.DisplayMeetingRatings ?? false,
          FeedbackStyle = conclusionActions.FeedbackStyle is null ? gqlFeedbackStyle.ALL_PARTICIPANTS : EnumHelper.ConvertToNonNullable<gqlFeedbackStyle>(conclusionActions.FeedbackStyle),
        };
      }
      catch (Exception)
      {
        //MeetingConclusionModel throw exeption when parameter LoadConclusionActions is set as false in L10AccesorMeetingData._LoadRecurrences()
        return MeetingConcludeActionsModel.Default();
      }
    }

    public static ConcludeActionsModel TransformConcludeActionsModelInConcludeMeeting(this ConcludeMeetingModel source)
    {
      return new ConcludeActionsModel
      {
        SendEmailSummaryTo = source.SendEmailSummaryTo,
        IncludeMeetingNotesInEmailSummary = source.IncludeMeetingNotesInEmailSummary,
        ArchiveCompletedTodos = source.ArchiveCompletedTodos,
        ArchiveHeadlines = source.ArchiveHeadlines,
        DisplayMeetingRatings = source.DisplayMeetingRatings,
        FeedbackStyle = source.FeedbackStyle
      };
    }

    internal static gqlPageType ConvertToPageType(this L10PageType pageType)
    {
      return pageType switch
      {
        L10PageType.Empty => gqlPageType.TITLE_PAGE,
        L10PageType.Segue => gqlPageType.CHECK_IN,
        L10PageType.Scorecard => gqlPageType.METRICS,
        L10PageType.Rocks => gqlPageType.GOALS,
        L10PageType.Headlines => gqlPageType.HEADLINES,
        L10PageType.Todo => gqlPageType.TODOS,
        L10PageType.IDS => gqlPageType.ISSUES,
        L10PageType.Conclude => gqlPageType.WRAP_UP,
        L10PageType.NotesBox => gqlPageType.NOTES_BOX,
        L10PageType.ExternalPage => gqlPageType.EXTERNAL_PAGE,
        L10PageType.Whiteboard => gqlPageType.WHITEBOARD,
        L10PageType.Html => gqlPageType.HTML,
        _ => throw new ArgumentException($"Unexpected value: {pageType} for argument: {nameof(pageType)}")
      };
    }

    public static MeetingPageQueryModel MeetingPageFromL10RecurrencePage(this L10Recurrence.L10Recurrence_Page source)
    {
      return new MeetingPageQueryModel
      {
        MeetingId = source.L10RecurrenceId,
        PageType = ConvertToPageType(source.PageType),
        NoteboxPadId = source.PadId,
        Minutes = source.Minutes,
        DateCreated = source.CreateTime.ToUnixTimeStamp(),
        DateDeleted = source.DeleteTime.ToUnixTimeStamp(),
        Id = source.Id,
        PageName = source.Title,
        Order = source._Ordering,
        ExpectedDurationS = source.Minutes * 60,
        Version = source.Version,
        LastUpdatedBy = source.LastUpdatedBy,
        DateLastModified = source.DateLastModified,
        ExternalPageUrl = source.Url,
        Subheading = source.Subheading,
        Timer = new TimerModel
        {
          TimeLastStarted = source.TimeLastStarted,
          TimePreviouslySpentS = source.TimePreviouslySpentS,
          TimeLastPaused = source.TimeLastPaused,
          TimeSpentPausedS = source.TimeSpentPausedS
        },
        Title = new MeetingPageTitleModel
        {
          NoteId = source.TitleNoteId,
          NoteText = source.TitleNoteText,
          Text = source.Title
        },
        CheckIn = new CheckInModel
        {
          CheckInType = ((gqlCheckInType)source.CheckInType).ToString(),
          IceBreaker = source.IceBreaker,
          IsAttendanceVisible = source.IsAttendanceVisible
        }
      };
    }

    public static L10Recurrence.L10Recurrence_Page L10RecurrencePageFromMeetingPage(CreateMeetingPageModel createPage = null, EditMeetingPageModel editPage = null, L10Recurrence.L10Recurrence_Page page = null)
    {
      if (createPage != null)
      {
        return new L10Recurrence.L10Recurrence_Page
        {
          L10RecurrenceId = createPage.RecurrenceId,
          PageType = (L10PageType)EnumHelper.ConvertToNonNullableEnum<gqlPageType>(createPage.PageType),
          Title = createPage.PageName,
          Minutes = createPage.ExpectedDurationS / 60
        };
      }

      var minutes = page.Minutes;
      if (editPage.ExpectedDurationS.HasValue)
        minutes = (decimal)editPage.ExpectedDurationS.Value;

      return new L10Recurrence.L10Recurrence_Page
      {
        Id = page.Id,
        L10RecurrenceId = page.L10RecurrenceId,
        PageType = (L10PageType?)EnumHelper.ConvertToNullableEnum<gqlPageType>(editPage.PageType) ?? page.PageType,
        Title = editPage?.PageName ?? page.Title,
        Minutes = minutes,
        TimeLastStarted = editPage?.timer?.TimeLastStarted ?? page.TimeLastStarted,
        TimePreviouslySpentS = editPage?.timer?.TimePreviouslySpentS ?? page.TimeSpentPausedS,
        TimeLastPaused = editPage?.timer?.TimeLastPaused ?? page.TimeLastPaused,
        TimeSpentPausedS = editPage?.timer?.TimeSpentPausedS ?? page.TimeSpentPausedS,
        Subheading = editPage?.Subheading ?? page.Subheading,
        Url = editPage?.ExternalPageUrl ?? page.Url,
      };
    }

    public static TrackedMetricQueryModel TransformTrackedMetric(this RadialReview.Models.TrackedMetricModel source)
    {
      return new TrackedMetricQueryModel()
      {
        Color = source.Color,
        CreatedTimestamp = source.CreatedTimestamp,
        DateLastModified = source.DateLastModified,
        DeleteTime = source.DeleteTime,
        Id = source.Id,
        LastUpdatedBy = source.LastUpdatedBy,
        MetricId = source.ScoreId,
        MetricTabId = source.MetricTabId,
        UserId = source.UserId,
        Version = source.Version
      };
    }

    public static MetricsTabQueryModel TransformMetricTab(this RadialReview.Models.MetricTabModel source)
    {
      return new MetricsTabQueryModel()
      {
        DateCreated = source.CreatedTimestamp,
        DateLastModified = source.DateLastModified,
        DeleteTime = source.DeleteTime,
        Id = source.Id,
        LastUpdatedBy = source.LastUpdatedBy,
        UserId = source.UserId,
        Version = source.Version,
        Frequency = source.Frequency,
        MeetingId = source.MeetingId,
        Name = source.Title,
        IsSharedToMeeting = source.ShareToMeeting,
        Units = source.Units
      };
    }

    public static GoalQueryModel TransformRock(this L10Meeting.L10Meeting_Rock source)
    {
      var goal = new GoalQueryModel
      {
        Id = source.ForRock.Id,
        RecurrenceId = source.ForRecurrence.Id,
        NotesId = source.ForRock.PadId,
        Title = source.ForRock.Name,
        Archived = source.ForRock.Archived,
        ArchivedTimestamp = source.ForRock.ArchivedTimestamp.ToUnixTimeStamp(),
        Status = GoalStatusFromRockStatus(source.ForRock.Completion),
        DateCreated = source.CreateTime.ToUnixTimeStamp(),
        DueDate = source.ForRock.DueDate.ToUnixTimeStamp(),
        Assignee = UserTransformer.TransformUser(source.ForRock.AccountableUser),
        Version = source.ForRock.Version,
        LastUpdatedBy = source.ForRock.LastUpdatedBy,
        DateLastModified = source.ForRock.DateLastModified,
        AddToDepartmentPlan = source.VtoRock,
      };

      if (source._recurrenceMilestoneSettings is not null)
      {
        goal.DepartmentPlanRecords = source._recurrenceMilestoneSettings.Select(x => x.ToGoalDepartmentPlanRecordQueryModel()).AsQueryable();
      }

      return goal;
    }

    public static GoalQueryModel TransformRock(this L10Recurrence.L10Recurrence_Rocks source)
    {

      var goal = new GoalQueryModel
      {
        Id = source.ForRock.Id,
        RecurrenceId = source.L10Recurrence.Id,
        NotesId = source.ForRock.PadId,
        Title = source.ForRock.Name,
        Archived = source.ForRock.Archived,
        ArchivedTimestamp = source.ForRock.ArchivedTimestamp.ToUnixTimeStamp(),
        Status = GoalStatusFromRockStatus(source.ForRock.Completion),
        DateCreated = source.CreateTime.ToUnixTimeStamp(),
        DueDate = source.ForRock.DueDate.ToUnixTimeStamp(),
        Assignee = UserTransformer.TransformUser(source.ForRock.AccountableUser),
        Version = source.ForRock.Version,
        LastUpdatedBy = source.ForRock.LastUpdatedBy,
        DateLastModified = source.ForRock.DateLastModified,
        AddToDepartmentPlan = source.VtoRock,
      };

      if (source._GoalRecurrenceRecords is not null)
      {
        goal.DepartmentPlanRecords = source._GoalRecurrenceRecords.Select(r => r.ToGoalDepartmentPlanRecordQueryModel()).AsQueryable();
      }

      return goal;
    }

    public static DepartmentPlanRecordQueryModel ToGoalDepartmentPlanRecordQueryModel(this L10Recurrence.GoalRecurrenceRecord source)
    {
      return new DepartmentPlanRecordQueryModel
      {
        Id = source.RecurrenceRockId,
        MeetingId = source.RecurrenceId,
        IsInDepartmentPlan = source.VtoRock
      };
    }

    public static GoalQueryModel TransformRock(this RadialReview.Models.Askables.RockModel source, long? recurrenceId = null)
    {
      var goal = new GoalQueryModel
      {
        Id = source.Id,
        RecurrenceId = recurrenceId ?? 0, //TODO how do we want to handle user's goals? should recurrenceId be a list?
        NotesId = source.PadId,
        Title = source.Name,
        Archived = source.Archived,
        ArchivedTimestamp = source.ArchivedTimestamp.ToUnixTimeStamp(),
        Status = GoalStatusFromRockStatus(source.Completion),
        DateCreated = source.CreateTime.ToUnixTimeStamp(),
        DueDate = source.DueDate.ToUnixTimeStamp(),
        Assignee = UserTransformer.TransformUser(source.AccountableUser),
        Version = source.Version,
        LastUpdatedBy = source.LastUpdatedBy,
        DateLastModified = source.DateLastModified,
        AddToDepartmentPlan = source._CompanyRock, // source._AddedToVTO,
      };

      if (source._GoalRecurenceRecords != null)
      {
        goal.DepartmentPlanRecords = source._GoalRecurenceRecords.Select(x => ToGoalDepartmentPlanRecordQueryModel(x)).AsQueryable();
      }
      return goal;
    }

    private static gqlGoalStatus GoalStatusFromRockStatus(RadialReview.Models.Enums.RockState status)
    {
      return
        status switch
        {
          RadialReview.Models.Enums.RockState.Complete => gqlGoalStatus.COMPLETED,
          RadialReview.Models.Enums.RockState.OnTrack => gqlGoalStatus.ON_TRACK,
          RadialReview.Models.Enums.RockState.AtRisk => gqlGoalStatus.OFF_TRACK,
          _ => gqlGoalStatus.OFF_TRACK,
        };
    }

    public static NotificationQueryModel TransformNotification(this RadialReview.Models.Notifications.NotificationModel notification)
    {
      return new NotificationQueryModel
      {
        Id = notification.Id,
        Text = notification.Name,
        UserId = notification.UserId,
        NotificationType = notification.Type.ToString(),
        ViewState = notification.DeleteTime != null ? "Archived" : notification.Seen != null ? "Viewed" : "New",
        DateCreated = notification.CreateTime.ToUnixTimeStamp(),
        Version = notification.Version,
        LastUpdatedBy = notification.LastUpdatedBy,
        DateLastModified = notification.DateLastModified,
        MeetingId = notification.MeetingId,
        MentionerId = notification.MentionerId,
        TodoId = notification.TodoId,
      };
    }

    public static OrgSettingsModel TransformUserOrgSettings(this RadialReview.Models.UserOrganizationModel source)
    {
      var Id = source.Organization.Id;
      var businessPlanId = L10Accessor.GetSharedVTOVision(source, source.Organization.Id);
      var orgSettings = source.GetOrganizationSettings();
      return new OrgSettingsModel
      {
        Id = Id,
        BusinessPlanId = businessPlanId.HasValue ? businessPlanId.Value : 0,
        V3BusinessPlanId = orgSettings.V3BusinessPlanId,
        WeekStart = orgSettings.WeekStart.ToString(),
        IsCoreProcessEnabled = orgSettings.EnableCoreProcess,
      };
    }

    public static OrganizationQueryModel TransformToOrganization(this RadialReview.Models.UserOrganizationModel source)
    {
      return new OrganizationQueryModel
      {
        Id = source.Organization.Id,
        UserId = source.Id,
        OrgName = source.Organization.Name,
        OrgImage = source.GetImageUrl()
      };
    }

    public static CommentQueryModel TransformComment(this RadialReview.Models.CommentModel x)
    {
      return
        new CommentQueryModel
        {
          Id = x.Id,
          Version = x.Version,
          PostedTimestamp = x.PostedDateTime.ToUnixTimeStamp(),
          DateCreated = x.PostedDateTime.ToUnixTimeStamp(),
          //Author = GetUserById(x.AuthorId, cancellationToken),
          AuthorId = x.AuthorId,
          Archived = x.DeleteTime != null,
          ArchivedDate = x.DeleteTime.ToUnixTimeStamp(),
          Body = x.Body,
          CommentParentType = x.CommentParentType,
          ParentId = x.ParentId,
        };
    }

    public static string VerifyAndTransformPageId(string pageId)
    {
      if (pageId.StartsWith("page-"))
        return pageId;

      return "page-" + pageId;
    }

    public static MeetingMetadataModel MeetingMetadataFromTinyRecurrence(this TinyRecurrence x, long userId)
    {
      return new MeetingMetadataModel
      {
        Id = x.Id,
        Name = x.Name,
        UserId = userId,
        //DateCreated = TODO
        //DateLastModified= DateTime.UtcNow, TODO
        FavoritedSortingPosition = (int)((x.StarDate.ToUnixTimeStamp() ?? 0) * 10),
        FavoritedTimestamp = x.StarDate.ToUnixTimeStamp(),
        MeetingType = x.MeetingType.ToString(),

        //Version = TODO
      };
    }


    public static MeetingLookupModel ToMeetingLookup(this V3TinyRecurrence tinyRec)
    {
      var meetingLookup = new MeetingLookupModel
      {
        Id = tinyRec.Id,
        Name = tinyRec.Name,
        IsCurrentUserAdmin = tinyRec.CurrentUserCanAdmin,
    };

      return meetingLookup;
    }

    public static HeadlineQueryModel TransformHeadline(this PeopleHeadline x)
    {
      return new HeadlineQueryModel()
      {
        Id = x.Id,
        Title = x.Message,
        Archived = x.DeleteTime != null,
        ArchivedTimestamp = x.DeleteTime.ToUnixTimeStamp(),
        NotesId = x.HeadlinePadId,
        UserId = x.OwnerId,
        RecurrenceId = x.RecurrenceId,
        Assignee = UserTransformer.TransformUser(x.Owner),
        Version = x.Version,
        LastUpdatedBy = x.LastUpdatedBy,
        DateLastModified = x.DateLastModified,
      };
    }

    public static MeetingNoteQueryModel MeetingNoteFromL10Note(this L10Note source)
    {
      return new MeetingNoteQueryModel
      {
        Id = source.Id,
        Title = source.Name,
        NotesId = source.PadId,
        Archived = source.DeleteTime != null,
        ArchivedTimestamp = source.DeleteTime.ToUnixTimeStamp(),
        Version = source.Version,
        LastUpdatedBy = source.LastUpdatedBy,
        DateLastModified = source.DateLastModified,
        OwnerId = source.OwnerId,
        DateCreated = source.CreateTime.ToUnixTimeStamp()
      };
    }

    public static HeadlineQueryModel HeadlineFromPeopleHeadline(this RadialReview.Models.L10.PeopleHeadline source, string notesText = null)
    {
      return new HeadlineQueryModel()
      {
        Id = source.Id,
        Title = source.Message,
        Archived = source.DeleteTime != null,
        ArchivedTimestamp = source.DeleteTime.ToUnixTimeStamp(),
        UserId = source.OwnerId,
        Assignee = UserTransformer.TransformUser(source.Owner),
        RecurrenceId = source.RecurrenceId,
        NotesId = source.HeadlinePadId,
        Version = source.Version,
        LastUpdatedBy = source.LastUpdatedBy,
        DateLastModified = source.DateLastModified,
        DateCreated = source.CreateTime.ToUnixTimeStamp(),
        NotesText = notesText
      };
    }

    public static MilestoneQueryModel TransformMilestone(this Milestone x)
    {
      return new MilestoneQueryModel()
      {
        Id = x.Id,
        DateCreated = x.CreateTime.ToUnixTimeStamp(),
        DateDeleted = x.DeleteTime.ToUnixTimeStamp(),
        Title = x.Name,
        DueDate = x.DueDate.ToUnixTimeStamp(),
        Completed = x.CompleteTime != null,
        status = x.bloomStatus.ToMilestoneStatus(),
        Version = x.Version,
        LastUpdatedBy = x.LastUpdatedBy,
        DateLastModified = x.DateLastModified,
        GoalId = x.RockId
      };
    }

    public static TodoQueryModel TransformTodo(this RadialReview.Models.Todo.TodoModel x, string? notesText = null)
    {
      return new TodoQueryModel()
      {
        Id = x.Id,
        Title = x.Message,
        DueDate = x.DueDate.ToUnixTimeStamp(),
        Completed = x.IsCompleted,
        CompletedTimestamp = x.CompleteTime.ToUnixTimeStamp(),
        Archived = x.CloseTime.HasValue || x.DeleteTime.HasValue,
        ArchivedTimestamp = x.CloseTime.ToUnixTimeStamp(),
        Assignee = UserTransformer.TransformUser(x.AccountableUser),
        NotesId = x.PadId,
        Version = x.Version,
        LastUpdatedBy = x.LastUpdatedBy,
        DateLastModified = x.DateLastModified,
        DateCreated = x.CreateTime.ToUnixTimeStamp(),
        Context = new ContextModel
        {
          FromNodeTitle = x.ContextNodeTitle,
          FromNodeType = x.ContextNodeType,
        },
        ForRecurrenceId = x.ForRecurrenceId == null ? 0 : (long)x.ForRecurrenceId,
        NotesText = notesText
        //Comments =
      };
    }

    public static TodoQueryModel TransformTodo(this AngularTodo x)
    {
      return new TodoQueryModel()
      {
        Id = x.Id,
        Title = x.Name,
        DueDate = x.DueDate.ToUnixTimeStamp(),
        Completed = x.Complete == true,
        CompletedTimestamp = x.CompleteTime.ToUnixTimeStamp(),
        Archived = x.CloseTime.HasValue || x.DeleteTime.HasValue,
        ArchivedTimestamp = x.CloseTime.ToUnixTimeStamp(),
        Assignee = UserTransformer.TransformUser(x.Owner),
        NotesId = x.GetPadId(),
        Version = x.Version,
        LastUpdatedBy = x.LastUpdatedBy,
        DateLastModified = x.DateLastModified,
        DateCreated = x.CreateTime.HasValue ? x.CreateTime.Value.ToUnixTimeStamp() : 0,
        Context = new ContextModel
        {
          // Not part of angular todo
        },
        ForRecurrenceId = x.L10RecurrenceId == null ? 0 : (long)x.L10RecurrenceId,
        //Comments =
      };
    }

    public static MeasurableQueryModel TransformMeasurable(this RadialReview.Models.Scorecard.MeasurableModel m)
    {
      var transform = new MeasurableQueryModel
      {
        Id = m.Id,
        Title = m.Title,
        Assignee = UserTransformer.TransformUser(m.AdminUser),
        Owner = UserTransformer.TransformUser(m.AccountableUser),
        Version = m.Version,
        LastUpdatedBy = m.LastUpdatedBy,
        DateLastModified = m.DateLastModified,
        DateCreated = m.CreateTime.ToUnixTimeStamp()
      };

      return transform;
    }

    public static MetricQueryModel TransformMeasurableToMetric(this RadialReview.Models.Scorecard.MeasurableModel source)
    {
      UserQueryModel userQueryModel = source.AccountableUser.TransformUser();

      MetricQueryModel result = new MetricQueryModel
      {
        Id = source.Id,
        Units = (gqlUnitType)source.UnitType,
        Rule = (gqlLessGreater)source.GoalDirection,
        SingleGoalValue = source.Goal == null ? null : $"{source.Goal}",
        MinGoalValue = source.Goal == null ? null : $"{source.Goal}",
        MaxGoalValue = source.AlternateGoal == null ? null : $"{source.AlternateGoal}",
        Frequency = source.Frequency.ToGqlMetricFrequency(),
        NotesId = source.NotesId,
        Formula = source.Formula,
        Archived = source.Archived,
        Title = source.Title,
        Assignee = userQueryModel,
        Owner = userQueryModel,
        DateCreated = source.CreateTime.ToUnixTimeStamp(),
        ShowCumulative = source.ShowCumulative,
        CumulativeRange = source.CumulativeRange,
        ShowAverage = source.ShowAverage,
        AverageRange = source.AverageRange,
        ProgressiveDate = source.ProgressiveDate,
        StartOfWeek = source.Organization.Settings.WeekStart,
      };

      if (source.AverageRange.HasValue)
      {
        result.AverageData = new MetricAverageDataModel
        {
          Average = source._Average,
          StartDate = source.AverageRange.ToUnixTimeStamp()
        };
      }

      if (source.CumulativeRange.HasValue)
      {
        result.CumulativeData = new MetricCumulativeDataModel
        {
          StartDate = source.CumulativeRange.ToUnixTimeStamp(),
          Sum = source._Cumulative
        };
      }

      if (source.ProgressiveDate.HasValue)
      {
        result.ProgressiveData = new MetricProgressiveDataModel
        {
          TargetDate = source.ProgressiveDate.ToUnixTimeStamp(),
          Sum = source._Progressive
        };
      }

      return result;
    }

    public static MetricDividerQueryModel Transform(this L10Recurrence.L10Recurrence_MetricDivider divider, int indexInTable = 0)
    {
      var result = new MetricDividerQueryModel {
        Id = divider.Id,
        Title = divider.Title,
        Height = divider.Height,
        IndexInTable = indexInTable
      };

      return result;
    }

    public static MetricQueryModelLookup TransformMeasurableToMetricLookup(this RadialReview.Models.Scorecard.MeasurableModel source)
    {
      MetricQueryModelLookup result = new MetricQueryModelLookup
      {
        Id = source.Id,
        Frequency = (gqlMetricFrequency)source.Frequency,
        Archived = source.Archived,
        Title = source.Title,
      };

      return result;
    }

    public static MetricQueryModel TransformL10RecurrenceMeasurable(this RadialReview.Models.L10.L10Recurrence.L10Recurrence_Measurable source)
    {
      var result = source.Measurable.TransformMeasurableToMetric();
      result.RecurrenceId = source.L10Recurrence.Id;
      result.IndexInTable = source.IndexInTable ?? source._Ordering;
      return result;
    }

    public static UserOrganizationQueryModel TransformUserOrganization(this RadialReview.Models.UserOrganizationModel x)
    {
      return new UserOrganizationQueryModel()
      {
        UserEmail = x.GetEmail(),
        UserOrganizationId = x.Id,
        UserId = x.User.NotNull(x => !string.IsNullOrEmpty(x.Id) ? Convert.ToInt32(x.Id) : 0),
        OrganizationId = x.Organization.NotNull(x => x.Id),
        //ResponsibilityId = x.Id,
        IsImplementer = x.IsImplementer,
        IsSuperAdmin = x.IsRadialAdmin,
        NumViewedNewFeatures = x.NumViewedNewFeatures,

      };
    }

    public static MetricFormulaLookupQueryModel TransformMeasurableToMetricFormulaLookup(this MeasurableModel measurable)
    {
      return new MetricFormulaLookupQueryModel()
      {
        Id = measurable.Id,
        DateCreated = measurable.CreateTime.ToUnixTimeStamp(),
        DateLastModified = measurable.DateLastModified,
        LastUpdatedBy = measurable.LastUpdatedBy,
        Version = measurable.Version,
        Title = measurable.Title,
        Frequency = Enum.Parse<gqlMetricFrequency>(measurable.Frequency.ToString(), true),
        Archived = measurable.Archived,
        Assignee = UserTransformer.TransformUser(measurable.AccountableUser),
      };
    }

    public static MetricAddExistingLookupQueryModel TransformMeasurableToMetricAddExistingLookup(this MeasurableModel measurable)
    {
      return new MetricAddExistingLookupQueryModel()
      {
        Id = measurable.Id,
        DateCreated = measurable.CreateTime.ToUnixTimeStamp(),
        DateLastModified = measurable.DateLastModified,
        LastUpdatedBy = measurable.LastUpdatedBy,
        Version = measurable.Version,
        Title = measurable.Title,
        Assignee = UserTransformer.TransformUser(measurable.AccountableUser),
      };
    }

    public static MeetingQueryModel TransformMeasurableRecurrenceToMeetingQueryModel(this L10Recurrence measurable)
    {
      return new MeetingQueryModel()
      {
        Id = measurable.Id,
        DateCreated = measurable.CreateTime.ToUnixTimeStamp(),
        DateLastModified = measurable.DateLastModified,
        LastUpdatedBy = measurable.LastUpdatedBy,
        Version = measurable.Version,
        Name = measurable.Name
      };
    }

    public static MetricScoreQueryModel TransformScore(this RadialReview.Models.Scorecard.ScoreModel x)
    {
      var timestamp = x.ForWeek.ToUnixTimeStamp();

      if (x.Measurable.Frequency == Frequency.WEEKLY)
        timestamp -= TimingUtility.SECONDS_IN_WEEK;

      return new MetricScoreQueryModel()
      {
        Id = x.Id,
        MeasurableId = x.MeasurableId,
        Value = x.Measured.ToString(),
        NotesText = x.NoteText,
        Timestamp = timestamp,
        DateCreated = x.DateEntered.ToUnixTimeStamp(),
        Version = x.Version,
        LastUpdatedBy = x.LastUpdatedBy,
        DateLastModified = x.DateLastModified,
      };
    }

    public static MetricCustomGoal TransformCustomGoal(this CustomGoalCreate customGoal)
    {
      return new MetricCustomGoal()
      {
        StartDate = customGoal?.StartDate,
        EndDate = customGoal?.EndDate,
        MaxGoalValue = customGoal?.MaxGoalValue,
        MinGoalValue = customGoal.MinGoalValue,
        Rule = (LessGreater)(EnumHelper.ConvertToNonNullableEnum<gqlLessGreater>(customGoal.Rule)),
        SingleGoalValue = customGoal?.SingleGoalValue,
      };
    }

    public static decimal TryParseStringToDecimal(string value)
    {
      decimal outDecimalValue;

      if (!decimal.TryParse(value ?? "0", out outDecimalValue))
        throw new Exception("At least one conversion failed. Could not convert all strings to decimals. Value: " + value);

      return outDecimalValue;
    }

    public static MetricCustomGoalQueryModel TransformMetricCustomGoal(this MetricCustomGoal customGoal)
    {
      return new MetricCustomGoalQueryModel()
      {
        Id = customGoal.Id,
        DateCreated = customGoal.CreateTime.ToUnixTimeStamp(),
        DateDeleted = customGoal.DeleteTime.ToUnixTimeStamp(),
        StartDate = customGoal.StartDate,
        EndDate = customGoal.EndDate,
        MaxGoalValue = customGoal.MaxGoalValue,
        MinGoalValue = customGoal.MinGoalValue,
        Rule = customGoal.Rule,
        SingleGoalValue = customGoal.SingleGoalValue,
        MeasurableId = (long)customGoal.MeasurableId,
      };
    }

    public static TermsQueryModel TransformTerms(this RadialReview.Core.Models.Terms.TermsCollection x)
    {
      if (x is null)
      {
        throw new ArgumentNullException(nameof(x));
      }

      return new TermsQueryModel()
      {
        WeeklyMeeting = x.GetTerm(Models.Terms.TermKey.WeeklyMeeting),
        CheckIn = x.GetTerm(Models.Terms.TermKey.CheckIn),
        Metrics = x.GetTerm(Models.Terms.TermKey.Metrics),
        Goals = x.GetTerm(Models.Terms.TermKey.Goals),
        Headlines = x.GetTerm(Models.Terms.TermKey.Headlines),
        ToDos = x.GetTerm(Models.Terms.TermKey.ToDos),
        Issues = x.GetTerm(Models.Terms.TermKey.Issues),
        WrapUp = x.GetTerm(Models.Terms.TermKey.WrapUp),
        BusinessPlan = x.GetTerm(Models.Terms.TermKey.BusinessPlan),
        DepartmentPlan = x.GetTerm(Models.Terms.TermKey.DepartmentPlan),
        FutureFocus = x.GetTerm(Models.Terms.TermKey.FutureFocus),
        ShortTermFocus = x.GetTerm(Models.Terms.TermKey.ShortTermFocus),
        LongTermIssues = x.GetTerm(Models.Terms.TermKey.LongTermIssues),
        OrganizationalChart = x.GetTerm(Models.Terms.TermKey.OrganizationalChart),
        OrgChart = x.GetTerm(Models.Terms.TermKey.OrgChart),
        CoreValues = x.GetTerm(Models.Terms.TermKey.CoreValues),
        Focus = x.GetTerm(Models.Terms.TermKey.Focus),
        BHAG = x.GetTerm(Models.Terms.TermKey.BHAG),
        MarketingStrategy = x.GetTerm(Models.Terms.TermKey.MarketingStrategy),
        Differentiators = x.GetTerm(Models.Terms.TermKey.Differentiators),
        ProvenProcess = x.GetTerm(Models.Terms.TermKey.ProvenProcess),
        Guarantee = x.GetTerm(Models.Terms.TermKey.Guarantee),
        TargetMarket = x.GetTerm(Models.Terms.TermKey.TargetMarket),
        Visionary = x.GetTerm(Models.Terms.TermKey.Visionary),
        SecondInCommand = x.GetTerm(Models.Terms.TermKey.SecondInCommand),
        ThreeYearVision = x.GetTerm(Models.Terms.TermKey.ThreeYearVision),
        OneYearGoals = x.GetTerm(Models.Terms.TermKey.OneYearGoals),
        LeadAndManage = x.GetTerm(Models.Terms.TermKey.LeadAndManage),
        QuarterlyPlanning = x.GetTerm(Models.Terms.TermKey.QuarterlyPlanning),
        AnnualPlanning = x.GetTerm(Models.Terms.TermKey.AnnualPlanning),
        Quarters = x.GetTerm(Models.Terms.TermKey.Quarters),
        EmpowerThroughChoice = x.GetTerm(Models.Terms.TermKey.EmpowerThroughChoice),
        Understand = x.GetTerm(Models.Terms.TermKey.Understand),
        Embrace = x.GetTerm(Models.Terms.TermKey.Embrace),
        Capacity = x.GetTerm(Models.Terms.TermKey.Capacity),
        ThinkOnTheBusiness = x.GetTerm(Models.Terms.TermKey.ThinkOnTheBusiness),
        Quarterly1_1 = x.GetTerm(Models.Terms.TermKey.Quarterly1_1),
        RightPersonRightSeat = x.GetTerm(Models.Terms.TermKey.RightPersonRightSeat),
        QuarterlyGoals = x.GetTerm(Models.Terms.TermKey.QuarterlyGoals),
        One_OneMeeting = x.GetTerm(Models.Terms.TermKey.One_OneMeeting),
        LaunchDay = x.GetTerm(Models.Terms.TermKey.LaunchDay),
        FutureFocusDay = x.GetTerm(Models.Terms.TermKey.FutureFocusDay),
        ShortTermFocusDay = x.GetTerm(Models.Terms.TermKey.ShortTermFocusDay),
        PurposeCausePassion = x.GetTerm(Models.Terms.TermKey.PurposeCausePassion),
        Measurables = x.GetTerm(Models.Terms.TermKey.Measurables),
        Milestones = x.GetTerm(Models.Terms.TermKey.Milestones),
        Niche = x.GetTerm(Models.Terms.TermKey.Niche)
      };
    }

    public static WorkspaceQueryModel TransformDashboard(this Dashboard d)
    {
      return new WorkspaceQueryModel()
      {
        Id = d.Id,
        Archived = d.DeleteTime.HasValue,
        Name = d.Title
      };
    }
  }
}