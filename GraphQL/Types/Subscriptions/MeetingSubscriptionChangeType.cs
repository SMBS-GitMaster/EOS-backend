using HotChocolate.Types;
using RadialReview.GraphQL.Models;
using System;
using System.Linq;
using RadialReview.Repositories;
using RadialReview.GraphQL.Types;
using RadialReview.Core.GraphQL.MeetingListLookup;
using RadialReview.Core.GraphQL.MetricFormulaLookup;
using RadialReview.Core.GraphQL.MetricAddExistingLookup;
using RadialReview.Core.GraphQL.BusinessPlan.Types.Queries;
using RadialReview.BusinessPlan.Core.Data.Models;
using RadialReview.BusinessPlan.Models;
using RadialReview.Core.GraphQL.BusinessPlan;
using RadialReview.Core.GraphQL.Models;

namespace RadialReview.Core.GraphQL.Types
{
  public class MeetingSubscriptionChangeType : ChangeType<IMeetingChange>
  {
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
      descriptor.Name(nameof(MeetingSubscriptionChangeType));

      ConfigureCreateUpdateDelete<MeetingQueryModel, MeetingChangeType, long>(descriptor, "Meeting");
      ConfigureCollections<MeetingQueryModel.Collections.Goal, GoalQueryModel, GoalChangeType, long>(descriptor, "Meeting", "Goal");
      ConfigureCollections<MeetingQueryModel.Collections.MeetingAttendee, MeetingAttendeeQueryModel, MeetingAttendeeChangeType, long>(descriptor, "Meeting", "MeetingAttendee");
      ConfigureCollections<MeetingQueryModel.Collections.Comment, CommentQueryModel, CommentChangeType, long>(descriptor, "Meeting", "Comment");
      ConfigureCollections<MeetingQueryModel.Collections.MeetingPage, MeetingPageQueryModel, MeetingPageChangeType, long>(descriptor, "Meeting", "MeetingPage");
      ConfigureCollections<MeetingQueryModel.Collections.MeetingRating, MeetingRatingModel, MeetingRatingChangeType, long>(descriptor, "Meeting", "MeetingRating");
      ConfigureCollections<MeetingQueryModel.Collections.MeetingNote, MeetingNoteQueryModel, MeetingNoteChangeType, long>(descriptor, "Meeting", "MeetingNote");
      ConfigureCollections<MeetingQueryModel.Collections.MetricDivider, MetricDividerQueryModel, MetricDividerChangeType, long>(descriptor, "Meeting", "MetricDivider");
      ConfigureCollections<MeetingQueryModel.Collections.Todo, TodoQueryModel, TodoChangeType, long>(descriptor, "Meeting", "Todo");
      ConfigureCollections<MeetingQueryModel.Collections.Todo, TodoQueryModel, TodoChangeType, long>(descriptor, "Meeting", "TodosActive");
      ConfigureCollections<MeetingQueryModel.Collections.Issue, IssueQueryModel, IssueChangeType, long>(descriptor, "Meeting", "Issue");
      ConfigureCollections<MeetingQueryModel.Collections.Headline, HeadlineQueryModel, HeadlineChangeType, long>(descriptor, "Meeting", "Headline");
      ConfigureCollections<MeetingQueryModel.Collections.MeetingInstance, MeetingInstanceQueryModel, MeetingInstanceChangeType, long>(descriptor, "Meeting", "MeetingInstance");
      ConfigureCollections<MeetingQueryModel.Collections.Metric, MetricQueryModel, MetricChangeType, long>(descriptor, "Meeting", "Metric");
      ConfigureCollections<MeetingQueryModel.Collections.IssueSentTo2, IssueSentToQueryModel, IssueSentToChangeType, long>(descriptor, "Meeting", "IssueSentTo");
      ConfigureCollections<MeetingQueryModel.Collections.IssueHistoryEntry2, IssueHistoryEntryQueryModel, IssueHistoryEntryChangeType, long>(descriptor, "Meeting", "IssueHistoryEntry");
      ConfigureCollections<MeetingQueryModel.Collections.Milestone2, MilestoneQueryModel, MilestoneChangeType, long>(descriptor, "Meeting", "Milestone");
      ConfigureCollections<MeetingQueryModel.Collections.Note, MeetingNoteQueryModel, MeetingNoteChangeType, long>(descriptor, "Meeting", "Note");
      ConfigureCollections<MeetingQueryModel.Collections.Metric, MeasurableQueryModel, MeasurableChangeType, long>(descriptor, "Meeting", "Measurable");
      ConfigureCollections<MeetingQueryModel.Collections.MetricTab, MetricsTabQueryModel, MetricTabChangeType, long>(descriptor, "Meeting", "MetricsTab");
      ConfigureCollections<MeetingQueryModel.Collections.MeetingAttendeeLookup1, MeetingAttendeeQueryModel, MeetingAttendeeLookupChangeType, long>(descriptor, "Meeting", "MeetingAttendeeLookup");
      ConfigureCollections<MeetingQueryModel.Collections.MeetingWorkspace, WorkspaceQueryModel, WorkspaceChangeType, long>(descriptor, "Meeting", "Workspace");

      ConfigureAssociation<MeetingQueryModel.Associations.OngoingMeeting, OngoingMeetingModel, ObjectType<OngoingMeetingModel>, long>(descriptor, "Meeting", "OngoingMeeting");
      ConfigureAssociation<MeetingQueryModel.Associations.CheckIn, CheckInModel, CheckInChangeType, long>(descriptor, "Meeting", "CheckIn");
      ConfigureAssociation<MeetingQueryModel.Associations.MeetingWorkspace2, WorkspaceQueryModel, WorkspaceChangeType, long>(descriptor, "Meeting", "Workspace");
      ConfigureAssociation<MeetingQueryModel.Associations.Segue, SegueModel, SegueChangeType, long>(descriptor, "Meeting", "Segue");
      ConfigureAssociation<MeetingQueryModel.Associations.User7, UserQueryModel, UserChangeType, long>(descriptor, "Meeting", "Owner");
      ConfigureAssociation<MeetingQueryModel.Associations.MetricTableColumnSettings, MetricTableColumnSettingsModel, ObjectType<MetricTableColumnSettingsModel>, long>(descriptor, "Meeting", "MetricTableColumnSettings");
      ConfigureAssociation<MeetingQueryModel.Associations.MeetingInstance2, MeetingInstanceQueryModel, MeetingInstanceChangeType, long>(descriptor, "Meeting", "MeetingInstance");
      ConfigureAssociation<MeetingQueryModel.Associations.MeetingAttendee3, MeetingAttendeeQueryModel, MeetingAttendeeChangeType, long>(descriptor, "Meeting", "MeetingAttendee");

      ConfigureCollections<MeetingQueryModel.Collections.MetricAddExistingLookup, MetricAddExistingLookupQueryModel, MetricAddExistingLookupChangeType, long>(descriptor, "Meeting", "MetricAddExistingLookup");
      ConfigureCreateUpdateDelete<MetricAddExistingLookupQueryModel, MetricAddExistingLookupChangeType, long>(descriptor, "MetricAddExistingLookup");
      ConfigureAssociation<MetricAddExistingLookupQueryModel.Associations.User17, UserQueryModel, UserChangeType, long>(descriptor, "MetricAddExistingLookup", "User");

      ConfigureCollections<MetricAddExistingLookupQueryModel.Collections.Meeting9, MeetingQueryModel, MeetingChangeType, long>(descriptor, "MetricAddExistingLookup", "Meeting");

      ConfigureCreateUpdateDelete<MeetingInstanceQueryModel, MeetingInstanceChangeType, long>(descriptor, "MeetingInstance");
      ConfigureCollections<MeetingInstanceQueryModel.Collections.MeetingNote2, MeetingNoteQueryModel, MeetingNoteChangeType, long>(descriptor, "MeetingInstance", "MeetingNote");
      ConfigureCollections<MeetingInstanceQueryModel.Collections.MeetingInstanceAttendee, MeetingInstanceAttendeeQueryModel, MeetingInstanceAttendeeChangeType, long>(descriptor, "MeetingInstance", "MeetingInstanceAttendee");
      ConfigureCollections<MeetingInstanceQueryModel.Collections.MeetingAttendee2, MeetingAttendeeQueryModel, MeetingAttendeeChangeType, long>(descriptor, "MeetingInstance", "MeetingAttendee");
      ConfigureAssociation<MeetingInstanceQueryModel.Associations.User10, UserQueryModel, UserChangeType, long>(descriptor, "MeetingInstance", "User"); // TODO:

      ConfigureCreateUpdateDelete<MeetingAttendeeQueryModel, MeetingAttendeeChangeType, long>(descriptor, "MeetingAttendee");
      ConfigureCollections<MeetingAttendeeQueryModel.Collections.MeetingInstanceAttendee2, MeetingInstanceQueryModel, MeetingInstanceChangeType, long>(descriptor, "MeetingAttendee", "MeetingInstanceAttendee");
      ConfigureAssociation<MeetingAttendeeQueryModel.Associations.User1, UserQueryModel, UserChangeType, long>(descriptor, "MeetingAttendee", "User");
      ConfigureAssociation<MeetingAttendeeQueryModel.Associations.MeetingInstanceAttendee3, MeetingInstanceAttendeeQueryModel, MeetingInstanceAttendeeChangeType, long>(descriptor, "MeetingAttendee", "MeetingInstanceAttendee");

      ConfigureCreateUpdateDelete<GoalQueryModel, GoalChangeType, long>(descriptor, "Goal");
      ConfigureCollections<GoalQueryModel.Collections.Milestone, MilestoneQueryModel, MilestoneChangeType, long>(descriptor, "Goal", "Milestone");
      ConfigureCollections<GoalQueryModel.Collections.Meeting4, GoalMeetingQueryModel, GoalMeetingChangeType, long>(descriptor, "Goal", "Meeting");
      ConfigureAssociation<GoalQueryModel.Associations.User5, UserQueryModel, UserChangeType, long>(descriptor, "Goal", "User");
      ConfigureCollections<GoalQueryModel.Collections.DepartmentPlanRecord, DepartmentPlanRecordQueryModel, DepartmentPlanRecordChangeType, long>(descriptor, "Goal", "DepartmentPlanRecord");
      ConfigureCreateUpdateDelete<DepartmentPlanRecordQueryModel, DepartmentPlanRecordChangeType, long>(descriptor, "DepartmentPlanRecord");

      ConfigureCreateUpdateDelete<MeetingInstanceAttendeeQueryModel, MeetingInstanceAttendeeChangeType, long>(descriptor, "MeetingInstanceAttendee");
      ConfigureAssociation<MeetingInstanceAttendeeQueryModel.Associations.User12, UserQueryModel, UserChangeType, long>(descriptor, "MeetingInstanceAttendee", "User");
      ConfigureAssociation<MeetingInstanceAttendeeQueryModel.Associations.MeetingAttendee4, MeetingAttendeeQueryModel, MeetingAttendeeChangeType, long>(descriptor, "MeetingInstanceAttendee", "MeetingAttendee");

      ConfigureCreateUpdateDelete<CommentQueryModel, CommentChangeType, long>(descriptor, "Comment");
      ConfigureAssociation<CommentQueryModel.Associations.User, UserQueryModel, UserChangeType, long>(descriptor, "Comment", "Author");

      ConfigureCreateUpdateDelete<MeetingPageQueryModel, MeetingPageChangeType, long>(descriptor, "MeetingPage");
      ConfigureAssociation<MeetingPageQueryModel.Associations.User8, UserQueryModel, UserChangeType, long>(descriptor, "MeetingPage", "User");

      ConfigureCreateUpdateDelete<MeetingRatingModel, MeetingRatingType, long>(descriptor, "MeetingRating");
      ConfigureAssociation<MeetingRatingModel.Associations.User3, UserQueryModel, UserChangeType, long>(descriptor, "MeetingRating", "User");

      ConfigureCreateUpdateDelete<MeetingNoteQueryModel, MeetingNoteChangeType, long>(descriptor, "MeetingNote");
      ConfigureAssociation<MeetingNoteQueryModel.Associations.User4, UserQueryModel, UserChangeType, long>(descriptor, "MeetingNote", "User");

      ConfigureCreateUpdateDelete<MetricDividerQueryModel, MetricDividerChangeType, long>(descriptor, "MetricDivider");
      ConfigureAssociation<MetricDividerQueryModel.Associations.User9, UserQueryModel, UserChangeType, long>(descriptor, "MetricDivider", "User");

      ConfigureCreateUpdateDelete<TodoQueryModel, TodoChangeType, long>(descriptor, "Todo");
      ConfigureCollections<TodoQueryModel.Collections.Comment2, CommentQueryModel, CommentChangeType, long>(descriptor, "Todo", "Comment");
      ConfigureAssociation<TodoQueryModel.Associations.User2, UserQueryModel, UserChangeType, long>(descriptor, "Todo", "User");
      ConfigureAssociation<TodoQueryModel.Associations.Meeting5, MeetingQueryModel, MeetingChangeType, long>(descriptor, "Todo", "Meeting");

      ConfigureCreateUpdateDelete<IssueQueryModel, IssueChangeType, long>(descriptor, "Issue");
      ConfigureCollections<IssueQueryModel.Collections.IssueSentTo, IssueSentToQueryModel, IssueChangeType, long>(descriptor, "Issue", "Issue");
      ConfigureCollections<IssueQueryModel.Collections.IssueHistoryEntry, IssueHistoryEntryQueryModel, IssueHistoryEntryChangeType, long>(descriptor, "Issue", "IssueHistoryEntry");
      ConfigureAssociation<IssueQueryModel.Associations.User14, UserQueryModel, UserChangeType, long>(descriptor, "Issue", "User");
      ConfigureAssociation<IssueQueryModel.Associations.Meeting, MeetingQueryModel, MeetingChangeType, long>(descriptor, "Issue", "Meeting");

      ConfigureAssociation<IssueQueryModel.Associations.Issue2, IssueQueryModel, IssueChangeType, long>(descriptor, "Issue", "Issue");

      ConfigureCreateUpdateDelete<HeadlineQueryModel, HeadlineChangeType, long>(descriptor, "Headline");
      ConfigureCollections<HeadlineQueryModel.Collections.Meeting8, MeetingQueryModel, MeetingChangeType, long>(descriptor, "Headline", "Meeting");
      ConfigureAssociation<HeadlineQueryModel.Associations.User6, UserQueryModel, UserChangeType, long>(descriptor, "Headline", "User");
      ConfigureAssociation<HeadlineQueryModel.Associations.Meeting10, MeetingQueryModel, MeetingChangeType, long>(descriptor, "Headline", "Meeting");

      ConfigureCreateUpdateDelete<MetricQueryModel, MetricChangeType, long>(descriptor, "Metric");
      ConfigureCollections<MetricQueryModel.Collections.MetricScore1, MetricScoreQueryModel, MetricScoreChangeType, long>(descriptor, "Metric", "MetricScore");
      ConfigureCollections<MetricQueryModel.Collections.Meeting7, MeetingQueryModel, MeetingChangeType, long>(descriptor, "Metric", "Meeting");
      ConfigureCollections<MetricQueryModel.Collections.MetricCustomGoal, MetricCustomGoalQueryModel, MetricCustomGoalChangeType, long>(descriptor, "Metric", "MetricCustomGoal");
      ConfigureAssociation<MetricQueryModel.Associations.MetricCumulativeData, MetricCumulativeDataType, MetricCumulativeDataChangeType, long>(descriptor, "Metric", "MetricCumulativeData");
      ConfigureAssociation<MetricQueryModel.Associations.MetricAverageData, MetricAverageDataType, MeetingAttendeeChangeType, long>(descriptor, "Metric", "MetricAverageData");
      ConfigureAssociation<MetricQueryModel.Associations.MetricProgressiveData, MetricProgressiveDataType, MetricProgressiveDataChangeType, long>(descriptor, "Metric", "MetricProgressiveData");
      ConfigureAssociation<MetricQueryModel.Associations.User13, UserQueryModel, UserChangeType, long>(descriptor, "Metric", "User");
      // NOTE: This associate is being written to remove exception from FE subscription.  Messages are not being sent for it on the BE currently.
      ConfigureAssociation<MetricQueryModel.Associations.MetricDivider1, MetricDividerQueryModel, MetricDividerChangeType, long>(descriptor, "Metric", "MetricDivider");

      ConfigureCreateUpdateDelete<IssueSentToQueryModel, IssueSentToChangeType, long>(descriptor, "IssueSentTo");
      ConfigureCollections<IssueSentToQueryModel.Collections.IssueHistoryEntry1, IssueHistoryEntryQueryModel, IssueHistoryEntryChangeType, long>(descriptor, "IssueSentTo", "IssueHistoryEntry");
      ConfigureAssociation<IssueSentToQueryModel.Associations.User11, UserQueryModel, UserChangeType, long>(descriptor, "IssueSentTo", "User");
      ConfigureAssociation<IssueSentToQueryModel.Associations.Issue1, IssueQueryModel, IssueChangeType, long>(descriptor, "IssueSentTo", "Issue");
      ConfigureAssociation<IssueSentToQueryModel.Associations.Meeting1, MeetingQueryModel, MeetingChangeType, long>(descriptor, "IssueSentTo", "Meeting");

      ConfigureCreateUpdateDelete<MilestoneQueryModel, MilestoneChangeType, long>(descriptor, "Milestone");

      ConfigureCreateUpdateDelete<MetricScoreQueryModel, MetricScoreChangeType, long>(descriptor, "MetricScore");

      ConfigureCreateUpdateDelete<MetricCustomGoalQueryModel, MetricCustomGoalChangeType, long>(descriptor, "MetricCustomGoal");

      ConfigureCreateUpdateDelete<IssueHistoryEntryQueryModel, IssueHistoryEntryChangeType, long>(descriptor, "IssueHistoryEntry");
      ConfigureAssociation<IssueHistoryEntryQueryModel.Associations.Meeting2, MeetingQueryModel, MeetingChangeType, long>(descriptor, "IssueHistoryEntry", "Meeting");

      ConfigureCreateUpdateDelete<UserQueryModel, UserChangeType, long>(descriptor, "User");
      ConfigureCollections<UserQueryModel.Collections.Meeting3, MeetingQueryModel, MeetingChangeType, long>(descriptor, "User", "Meeting");
      ConfigureCollections<UserQueryModel.Collections.Notification1, NotificationQueryModel, NotificationChangeType, long>(descriptor, "User", "Notification");

      ConfigureCollections<UserQueryModel.Collections.Workspace1, WorkspaceQueryModel, WorkspaceChangeType, long>(descriptor, "User", "Workspace");
      ConfigureAssociation<UserQueryModel.Associations.Workspaces, WorkspaceQueryModel, WorkspaceChangeType, long>(descriptor, "User", "Workspace");

      ConfigureCollections<UserQueryModel.Collections.MeetingPermissionLookup, SelectListQueryModel, SelectListChangeType, long>(descriptor, "User", "MeetingPermissionLookup");
      ConfigureCollections<UserQueryModel.Collections.MetricFormulaLookup, MetricFormulaLookupQueryModel, MetricFormulaLookupChangeType, long>(descriptor, "User", "MetricFormulaLookup");

      ConfigureCreateUpdateDelete<MetricFormulaLookupQueryModel, MetricFormulaLookupChangeType, long>(descriptor, "MetricFormulaLookup");
      ConfigureAssociation<MetricFormulaLookupQueryModel.Associations.User16, UserQueryModel, UserChangeType, long>(descriptor, "MetricFormulaLookup", "User");

      ConfigureAssociation<UserQueryModel.Associations.OrgSettings, OrgSettingsModel, OrgSettingsChangeType, long>(descriptor, "User", "OrgSettings");
      ConfigureCollections<UserQueryModel.Collections.UserNodeTodos, TodoQueryModel, TodoChangeType, long>(descriptor, "User", "Todo");
      ConfigureCollections<UserQueryModel.Collections.UserGoal, GoalQueryModel, GoalChangeType, long>(descriptor, "User", "Goal");
      ConfigureAssociation<UserQueryModel.Associations.UserGoals, GoalQueryModel, GoalChangeType, long>(descriptor, "User", "Goal");
      ConfigureCollections<UserQueryModel.Collections.UserMetric, MetricQueryModel, MetricChangeType, long>(descriptor, "User", "Metric");
      ConfigureAssociation<UserQueryModel.Associations.UserMetrics, MetricQueryModel, MetricChangeType, long>(descriptor, "User", "Metric");

      ConfigureCreateUpdateDelete<MeetingListLookupModel, MeetingListLookupChangeType, long>(descriptor, "MeetingListLookup");
      ConfigureCollections<UserQueryModel.Collections.MeetingListLookup, MeetingListLookupModel, MeetingListLookupChangeType, long>(descriptor, "User", "MeetingListLookup");

      ConfigureCreateUpdateDelete<SelectListQueryModel, SelectListChangeType, long>(descriptor, "MeetingPermissionLookup");

      ConfigureCreateUpdateDelete<MeetingAttendeeQueryModelLookup, MeetingAttendeeLookupChangeType, long>(descriptor, "MeetingAttendeeLookup");
      ConfigureCollections<MeetingListLookupModel.Collections.MeetingAttendeeLookup, MeetingAttendeeQueryModelLookup, MeetingAttendeeLookupChangeType, long>(descriptor, "MeetingListLookup", "MeetingAttendeeLookup");

      ConfigureCreateUpdateDelete<BloomLookupModel, BloomLookupChangeType, long>(descriptor, "BloomLookupNode");

      ConfigureCreateUpdateDelete<MetricsTabQueryModel, MetricTabChangeType, long>(descriptor, "MetricsTab");
      ConfigureCollections<MetricsTabQueryModel.Collections.TrackedMetric, TrackedMetricQueryModel, TrackedMetricChangeType, long>(descriptor, "MetricsTab", "TrackedMetric");
      ConfigureAssociation<MetricsTabQueryModel.Associations.Meeting6, MeetingQueryModel, MeetingChangeType, long>(descriptor, "MetricsTab", "Meeting");

      ConfigureAssociation<MetricsTabQueryModel.Associations.User15, UserQueryModel, UserChangeType, long>(descriptor, "MetricsTab", "User");

      ConfigureCreateUpdateDelete<TrackedMetricQueryModel, TrackedMetricChangeType, long>(descriptor, "TrackedMetric");
      ConfigureAssociation<TrackedMetricQueryModel.Associations.Metric2, MetricQueryModel, MetricChangeType, long>(descriptor, "TrackedMetric", "Metric");

      ConfigureCreateUpdateDelete<WorkspaceTileQueryModel, WorkspaceTileChangeType, long>(descriptor, "WorkspaceTile");

      ConfigureCreateUpdateDelete<WorkspaceQueryModel, WorkspaceChangeType, long>(descriptor, "Workspace");
      ConfigureAssociation<WorkspaceQueryModel.Associations.WorkspaceUser, WorkspaceTileQueryModel, WorkspaceTileChangeType, long>(descriptor, "Workspace", "User");
      ConfigureAssociation<WorkspaceQueryModel.Associations.TileNodes2, WorkspaceTileQueryModel, WorkspaceTileChangeType, long>(descriptor, "Workspace", "WorkspaceTile");
      ConfigureCollections<WorkspaceQueryModel.Collections.TileNodes, WorkspaceTileQueryModel, WorkspaceTileChangeType, long>(descriptor, "Workspace", "WorkspaceTile");
      ConfigureCollections<WorkspaceQueryModel.Collections.PersonalNotes, WorkspaceNoteQueryModel, WorkspaceNoteChangeType, long>(descriptor, "Workspace", "WorkspaceNote");
      ConfigureAssociation<WorkspaceQueryModel.Associations.PersonalNotes2, WorkspaceNoteQueryModel, WorkspaceNoteChangeType, long>(descriptor, "Workspace", "WorkspaceNote");

      ConfigureCreateUpdateDelete<WorkspaceNoteQueryModel, WorkspaceNoteChangeType, long>(descriptor, "WorkspaceNote");


      ConfigureCreateUpdateDelete<OrgSettingsModel, OrgSettingsChangeType, long>(descriptor, "OrgSettings");
      ConfigureCreateUpdateDelete<NotificationQueryModel, NotificationChangeType, long>(descriptor, "Notification");

      #region orgChartSeat
      ConfigureCreateUpdateDelete<OrgChartPositionQueryModel, PositionChangeType, long>(descriptor, "OrgChartPosition");
      ConfigureCreateUpdateDelete<OrgChartPositionRoleQueryModel, PositionRoleChangeType, long>(descriptor, "OrgChartPositionRole");
      ConfigureCollections<OrgChartPositionQueryModel.Collections.OrgChartPositionRole, OrgChartPositionRoleQueryModel, PositionRoleChangeType, long>(descriptor, "OrgChartPosition", "OrgChartPositionRole");
      ConfigureCollections<OrgChartSeatQueryModel.Collections.User18, UserQueryModel, UserChangeType, long>(descriptor, "OrgChartSeat", "User");
      ConfigureCollections<OrgChartSeatQueryModel.Collections.OrgChartSeat, OrgChartSeatQueryModel, OrgChartSeatChangeType, long>(descriptor, "OrgChartSeat", "OrgChartSeat");
      ConfigureCollections<OrgChartQueryModel.Collections.OrgChartSeat2, OrgChartSeatQueryModel, OrgChartSeatChangeType, long>(descriptor, "OrgChart", "OrgChartSeat");
      ConfigureCreateUpdateDelete<OrgChartQueryModel, OrgChartChangeType, long>(descriptor, "OrgChart");
      ConfigureAssociation<UserQueryModel.Associations.OrgChart, OrgChartQueryModel, OrgChartChangeType, long>(descriptor, "User", "OrgChart");
      ConfigureAssociation<OrgChartSeatQueryModel.Associations.OrgChartPosition, OrgChartPositionQueryModel, PositionChangeType, long>(descriptor, "OrgChartSeat", "OrgChartPosition");
      ConfigureCreateUpdateDelete<OrgChartSeatQueryModel, OrgChartSeatChangeType, long>(descriptor, "OrgChartSeat");

      #endregion

      #region BusinessPlan

      ConfigureCreateUpdateDelete<BusinessPlanModel, BusinessPlanChangeType, long>(descriptor, "BusinessPlan");

      ConfigureCreateUpdateDelete<BusinessPlanTile, BusinessPlanTileChangeType, Guid>(descriptor, "BusinessPlanTile");
      ConfigureCollections<BusinessPlanModel.Collections.BusinessPlanTileBusiness, BusinessPlanTile, BusinessPlanTileChangeType, Guid>(descriptor, "BusinessPlan", "BusinessPlanTile");

      ConfigureCreateUpdateDelete<BusinessPlanListItem, BusinessPlanItemChangeType, Guid>(descriptor, "BusinessPlanListItem");
      ConfigureCollections<BusinessPlanListCollectionQueryModel.Collections.BusinessPlanListItemCollection, BusinessPlanListItem, BusinessPlanItemChangeType, Guid>(descriptor, "BusinessPlanListCollection", "BusinessPlanListItem");

      ConfigureCreateUpdateDelete<BusinessPlanListCollection, BusinessPlanListCollectionChangeType, Guid>(descriptor, "BusinessPlanListCollection");
      ConfigureCollections<BusinessPlanModel.Collections.BusinessPlanListCollectionTile, BusinessPlanListCollection, BusinessPlanListCollectionChangeType, Guid>(descriptor, "BusinessPlanTile", "BusinessPlanListCollection");
     

      #endregion

    }

  }
}
