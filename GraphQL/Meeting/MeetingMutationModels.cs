using HotChocolate;
using HotChocolate.Types;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models.Mutations
{
  public class MeetingCreateModel
  {
    public string Name { get; set; }

    public string AgendaType { get; set; }

    public string MeetingType { get; set; }

    [DefaultValue(null)]
    public List<UserPermissions> AttendeeIdByPermissions { get; set; }
    [DefaultValue(null)]
    public List<UserPermissions> MemberIdByPermissions { get; set; }

    [DefaultValue(null)]
    public string VideoConferenceLink { get; set; }

    [DefaultValue(0)]
    public double ScheduledStartTime { get; set; }

    [DefaultValue(0)]
    public double ScheduledEndTime { get; set; }
  }

  public class CheckinModel
  {
    [DefaultValue(null)]
    public string Type { get; set; }
    [DefaultValue(null)]
    public string IceBreaker { get; set; }
    [DefaultValue(true)]
    public bool IsAttendanceVisible { get; set; }
  }

  public class MetricTableColumnSettings
  {
    [DefaultValue(null)]
    public bool? Owner { get; set; }
    [DefaultValue(null)]
    public bool? Goal { get; set; }
    [DefaultValue(null)]
    public bool? Cumulative { get; set; }
    [DefaultValue(null)]
    public bool? Average { get; set; }
  }

  public class MeetingEditModel
  {

    public long MeetingId { get; set; }

    [DefaultValue(null)] public string Name { get; set; }

    [DefaultValue(null)] public string VideoConferenceLink { get; set; }

    [DefaultValue(null)] public double? ScheduledStartTime { get; set; }

    [DefaultValue(null)] public double? ScheduledEndTime { get; set; }

    [DefaultValue(null)] public string IssueVoting { get; set; }

    [DefaultValue(null)] public ConcludeActionsModel ConcludeActions { get; set; }

    [DefaultValue(null)] public long[] Attendees { get; set; }

    [DefaultValue(null)] public long[] MeetingPages { get; set; }

    [DefaultValue(null)] public long[] SpecialMeetingSessions { get; set; }

    [DefaultValue(null)] public long[] MeetingInstances { get; set; }

    [DefaultValue(null)] public double? LastViewedTimestamp { get; set; }

    [DefaultValue(null)] public double? MetricTableWidthDragScrollPct { get; set; }

    [DefaultValue(null)] public MetricTableColumnSettings MetricTableColumnToIsVisibleSettings { get; set; }

    [DefaultValue(null)] public bool? ReverseMetrics { get; set; }

    [DefaultValue(null)] public bool? ShowNumberedIssueList { get; set; }

    [DefaultValue (null)] public long? BusinessPlanId { get; set; }

  }

  public class MeetingEditConcludeActionsModel {
    public long MeetingId { get; set; }
    [DefaultValue(null)] public ConcludeActionsModel ConcludeActions { get; set; }
  }

  public class ConcludeActionsModel
  {
    [DefaultValue(null)]
    public string SendEmailSummaryTo { get; set; }
    [DefaultValue(null)]
    public bool? IncludeMeetingNotesInEmailSummary { get; set; }
    [DefaultValue(null)]
    public bool? ArchiveCompletedTodos { get; set; }
    [DefaultValue(null)]
    public bool? ArchiveHeadlines { get; set; }
    [DefaultValue(null)]
    public bool? ArchiveCompletedIssues { get; set; }
    [DefaultValue(null)]
    public bool? DisplayMeetingRatings { get; set; }
    [DefaultValue(null)]
    public string FeedbackStyle { get; set; }
  }

  public class MeetingEditMeetingInstanceModel
  {
    public long MeetingInstanceId { get; set; }
    [DefaultValue(null)] public long? LeaderId { get; set; }
    [DefaultValue(null)] public long? CurrentPageId { get; set; }
    [DefaultValue(null)] public bool? issueVotingHasEnded { get; set; }
    [DefaultValue(null)] public long[]? SelectedNotes { get; set; }
  }

  public class MeetingEditLastViewedTimestampModel
  {
    public long MeetingId { get; set; }
    [DefaultValue(null)] public double? LastViewedTimestamp { get; set; }
  }

  public class UserPermissions
  {
    public long Id { get; set; }

    [GraphQLNonNullType]
    public MeetingPermissionsModel permissions { get; set; }
  }

}
