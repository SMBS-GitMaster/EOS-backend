namespace RadialReview.Core.GraphQL.Common.Constants
{
  public class ResourceNames
  {
    public const string StartMeeting = "startMeeting";

    public const string HeadlineEvents = "headlineEvents";

    public const string GoalsEvents = "goalsEvents";

    public const string TodoEvents = "todoEvents";

    public const string IssueEvents = "issueEvents";

    public const string MeasurableEvents = "measurableEvents";

    public const string RateMeetingEvents = "rateMeetingEvents";

    public const string WrapUpEvents = "wrapUpEvents";

    public const string MilestoneEvents = "milestoneEvents";

    public const string ScoreEvents = "scoreEvents";

    public const string MeetingAttendeeEvents = "meetingAttendeeEvents";

    public const string CheckInEvents = "checkInEvents";

    public const string WorkspaceEvents = "workspaceEvents";

    public const string WorkspaceTileEvents = "workspaceTileEvents";

    public const string WorkspaceNoteEvents = "workspaceNoteEvents";


    public static string Meeting(long id) => $"meeting_{id}";
    public static string MeetingAttendee(long id) => $"meeting_attendee_{id}";
    public static string Headline(long id) => $"headline_{id}";
    public static string Issue(long id) => $"issue_{id}";
    public static string Todo(long id) => "todo_{id}";
    public static string Goal(long id) => $"goal{id}";
    public static string Metric(long id) => $"measurable_{id}";
    public static string MetricDivider(long id) => $"metric_divider_{id}";
    public static string Milestone(long id) => $"milestone_{id}";
    public static string MetricsTab(long id) => $"metrics_tab_{id}";
    public static string CheckIn(long id) => $"l10RecurrencePage_{id}";

    public static string User(long id) => $"user_{id}";
    public static string OrgChart(long id) => $"org_chart_id{0}";
    public static string BloomlookupNode(long id) => $"bloomlookupNode_{id}";

    public static string Workspace(long id) => $"workspace_{id}";

    public static string WorkspaceTile(long id) => $"workspace_tile_{id}";

    public static string WorkspaceNote(long id) => $"workspace_note{id}";


    public static readonly string Users = $"users";
    public static readonly string Meetings = $"meetings";

    // Business plan
    public static readonly string BusinessPlans = "businessPlans";
    public static string BusinessPlan(long id) => $"businessPlan_{id}";
    //
  }
}
