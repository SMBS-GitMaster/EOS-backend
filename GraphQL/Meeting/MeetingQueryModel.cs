using System;
using System.Linq;
using System.Collections.Generic;
using RadialReview.Core.GraphQL.Enumerations;

namespace RadialReview.GraphQL.Models
{
  public class MeetingQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }

    #endregion

    #region Properties

    public string Name { get; set; }
    public long OrgId { get; set; }

    public long? UserOrgId { get; set; }

    public string OrgName { get; set; }

    public double CreatedTimestamp { get; set; }

    public long? FavoriteId { get; set; }

    public double? FavoritedTimestamp { get; set; }

    public int? FavoritedSortingPosition { get; set; }

    public string VideoConferenceLink { get; set; }

    public bool UserIsAttendee { get; set; }

    public bool Archived { get; set; }

    public string MeetingType { get; set; }

    public double? ScheduledStartTime { get; set; }

    public double? ScheduledEndTime { get; set; }

    public decimal ExpectedMeetingDurationFromAgendaInMinutes { get; set; }

    public string IssueVoting { get; set; }

    public string StartOfWeekOverride { get; set; }

    public long? BusinessPlanId { get; set; }

    public long MetricTableWidthDragScrollPct { get; set; }

    public MetricTableColumnSettingsModel MetricTableColumnToIsVisibleSettings { get; set; }

    public bool HighlightPreviousWeekForMetrics { get; set; }

    public bool ReverseMetrics { get; set; }

    public bool PreventEditingUnownedMetrics { get; set; }

    public double? LastViewedTimestamp { get; set; }

    public MeetingInstanceQueryModel CurrentMeetingInstance { get; set; }

    public long? CurrentMeetingInstanceId { get; set; }

    public OngoingMeetingModel Ongoing { get; set; }

    public bool ShowNumberedIssueList { get; set; }

    public IQueryable<MeetingAttendeeQueryModel> Attendees { get; set; }
    public IQueryable<MeetingAttendeeQueryModelLookup> AttendeesLookup { get; set; }

    public IQueryable<MeetingPageQueryModel> MeetingPages { get; set; }

    public IQueryable<GoalQueryModel> Goals { get; set; }

    public WorkspaceQueryModel Workspace { get; set; }

    public IQueryable<HeadlineQueryModel> Headlines { get; set; }

    public IQueryable<IssueQueryModel> Issues { get; set; }

    public IQueryable<MeetingNoteQueryModel> Notes { get; internal set; }

    public long UserId { get; set; }

    public string Email { get; set; }

    public UserQueryModel Owner { get; set; }

    public MeetingInstanceQueryModel NextMeetingInstance { get; set; }

    public MeetingConcludeActionsModel ConcludeActions { get; set; }

    public IQueryable<UserQueryModel> CreateIssueAssignees { get; set; }

    public IQueryable<UserQueryModel> EditIssueAssignees { get; set; }

    public IQueryable<MeetingQueryModel> EditIssueMeetings { get; set; }

    public IQueryable<MeetingQueryModel> CreateHeadlineAssignees { get; set; }

    public IQueryable<MeetingQueryModel> EditHeadlineAssignees { get; set; }

    public List<MeetingMetadataModel> EditHeadlineMeetings { get; set; }

    public IQueryable<MeetingQueryModel> CreateTodoAssignees { get; set; }

    public IQueryable<MeetingQueryModel> EditTodoAssignees { get; set; }

    #endregion

    #region

    public static class Collections
    {
      public enum Goal
      {
        Goals
      }

      public enum MeetingAttendee
      {
        Attendees
      }

      public enum MeetingAttendeeLookup1
      {
        MeetingAttendeeLookups
      }

      public enum Comment
      {
        Comments
      }

      public enum MeetingPage
      {
        MeetingPages
      }

      public enum MeetingRating
      {
        AttendeeInstances
      }

      public enum MeetingNote
      {
        Notes
      }

      public enum MetricDivider
      {
        MetricDividers
      }

      public enum Todo
      {
        Todos,
        TodosActives
      }

      public enum Issue
      {
        Issues,
        LongTermIssues
      }

      public enum Headline
      {
        Headlines
      }

      public enum MeetingInstance
      {
        MeetingInstances
      }

      public enum IssueSentTo2
      {
        IssuesSentTo
      }

      public enum IssueHistoryEntry2
      {
        IssueHistoryEntries
      }

      public enum Milestone2
      {
        Milestone
      }

      public enum Note
      {
        Notes
      }

      public enum Metric
      {
        Metrics
      }

      public enum MetricTab
      {
        MetricsTabs
      }
      public enum MetricAddExistingLookup
      {
        MetricAddExistingLookup
      }

      public enum MeetingWorkspace
      {
        MeetingWorkspace
      }
    }

    public static class Associations
    {
      public enum OngoingMeeting
      {
        Ongoing
      }

      public enum CheckIn
      {
        CheckIn
      }

      public enum MeetingWorkspace2
      {
        Workspace
      }

      public enum Segue
      {
        Segue
      }

      public enum User7
      {
        Owner
      }

      public enum MetricTableColumnSettings
      {
        MetricTableColumnToIsVisibleSettings
      }

      public enum MeetingInstance2
      {
        CurrentMeetingInstance
      }

      public enum MeetingAttendee3
      {
        MeetingAttendee
      }
    }

    #endregion
  }
}