namespace RadialReview.GraphQL.Types {
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.Core.GraphQL.MetricAddExistingLookup;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using System.Threading;

  public class MeetingChangeType : MeetingQueryType {
    public MeetingChangeType() : base(true) {
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingQueryModel> descriptor) {
      base.Configure(descriptor);

      descriptor.Name("MeetingModelChange");
    }
  }

  public class MeetingQueryType : ObjectType<MeetingQueryModel> {
    protected readonly bool isSubscription;

    public MeetingQueryType()
      : this(false) {
    }

    protected MeetingQueryType(bool isSubscription) {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingQueryModel> descriptor) {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "meeting");

      descriptor
        .Field(t => t.Id)
        .Type<LongType>()
        .IsProjected(true);

      descriptor
        .Field(t => t.CurrentMeetingInstanceId)
        .Type<LongType>()
        .IsProjected(true);

      descriptor
        .Field("averageMeetingRating")
        .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetAverageMeetingRatingAsync(ctx.Parent<MeetingQueryModel>().Id, ct))
        ;

      descriptor
        .Field("editIssueMeetings")
        .Type<ListType<IdNamePairQueryType>>()
        .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetEditIssueMeetings(ctx.Parent<MeetingQueryModel>().Id, ct))
        .UsePaging<IdNamePairQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        ;

      if (isSubscription) {
        descriptor
          .Field("goals")
          .Type<ListType<GoalChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, GoalQueryModel>(async (keys, cancellationToken) => {
            var result = await ctx.Service<IDataContext>().GetGoalsForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_goals").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("attendees")
          .Type<ListType<MeetingAttendeeChangeType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingAttendeesAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("attendeesLookup")
          .Type<ListType<MeetingAttendeeLookupChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMeetingAttendeesLookupAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("currentMeetingInstance")
          .Type<MeetingInstanceChangeType>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetInstanceForMeetingAsync(ctx.Parent<MeetingQueryModel>().CurrentMeetingInstanceId, ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("createIssueAssignees")
          .Type<ListType<MeetingAttendeeChangeType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingAttendeesAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("editIssueAssignees")
          .Type<ListType<MeetingAttendeeChangeType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingAttendeesAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("createTodoAssignees")
          .Type<ListType<MeetingAttendeeChangeType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingAttendeesAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("editTodoAssignees")
          .Type<ListType<MeetingAttendeeChangeType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingAttendeesAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();


        descriptor
          .Field("comments")
          .Type<ListType<CommentChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetCommentsAsync(RadialReview.Models.ParentType.Issue, ctx.Parent<IssueQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;

        descriptor
          .Field("meetingPages")
          .Type<ListType<MeetingPageChangeType>>()
          .Resolve(ctx => ctx.Parent<MeetingQueryModel>().MeetingPages)
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("meetingRatings")
          .Type<ListType<MeetingRatingChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMeetingAttendeeInstancesForMeetingsAsync(new[] { ctx.Parent<MeetingQueryModel>().Id }, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("notes")
          .Type<ListType<MeetingNoteChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetNotesForMeetingsAsync(new[] { ctx.Parent<MeetingQueryModel>().Id }, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("metricDividers")
          .Type<ListType<MetricDividerChangeType>>()
          .Resolve(async (ctx, ct) => (await ctx.Service<IDataContext>().GetMetricDividersForMeetingsAsync([ctx.Parent<MeetingQueryModel>().Id], ct)).Select(x => x.divider))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("todos")
          .Type<ListType<TodoChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetTodosForMeetingsAsync(new[] { ctx.Parent<MeetingQueryModel>().Id }, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("todosActives")
          .Type<ListType<TodoChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetTodosForMeetingsAsync(new[] { ctx.Parent<MeetingQueryModel>().Id }, cancellationToken, onlyActives: true))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("issues")
          .Type<ListType<IssueChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, IssueQueryModel>(async (keys, cancellationToken) => {
            var result = await ctx.Service<IDataContext>().GetIssuesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_issues").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("longTermIssues")
          .Type<ListType<IssueChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, IssueQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetLongTermIssuesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_longTermIssues").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("sentToIssues")
          .Type<ListType<IssueChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, IssueQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetSentToIssuesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_sentToIssues").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();


        descriptor
          .Field("archivedIssues")
          .Type<ListType<IssueChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, IssueQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetArchivedIssuesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_archivedIssues").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("recentlySolvedIssues")
          .Type<ListType<IssueChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, IssueQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetRecentlySolvedIssuesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_recentlySolvedIssues").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();


        descriptor
          .Field("headlines")
          .Type<ListType<HeadlineChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, HeadlineQueryModel>(async (keys, cancellationToken) => {
            var result = await ctx.Service<IDataContext>().GetHeadlinesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_headlines").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;

        descriptor
          .Field("workspace")
          .Type<WorkspaceChangeType>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetWorkspaceForMeetingAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection();

        descriptor
          .Field("meetingInstances")
          .Type<ListType<MeetingInstanceChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MeetingInstanceQueryModel>(async (keys, cancellationToken) => {
            var result = await ctx.Service<IDataContext>().GetInstancesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_meetingInstances").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("metrics")
          .Type<ListType<MetricChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricsForMeetingAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("weeklyMetricsLookup")
          .Type<ListType<MetricLookupChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricsForMeetingLookupAsync(ctx.Parent<MeetingQueryModel>().Id, "WEEKLY", cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("monthlyMetricsLookup")
          .Type<ListType<MetricLookupChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricsForMeetingLookupAsync(ctx.Parent<MeetingQueryModel>().Id, "MONTHLY", cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();
        descriptor
          .Field("quarterlyMetricsLookup")
          .Type<ListType<MetricLookupChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricsForMeetingLookupAsync(ctx.Parent<MeetingQueryModel>().Id, "QUARTERLY", cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("metricAddExistingLookup")
          .Type<ListType<MetricAddExistingLookupChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricAddExistingLookup(cancellationToken, ctx.Parent<MeetingQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("issuesSentTo")
          .Type<ListType<IssueSentToChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetIssuesSentToForMeetingAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("metricsTabs")
          .Type<ListType<MetricTabChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricTabsForMeeting(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("createHeadlineAssignees")
          .Type<ListType<MeetingAttendeeChangeType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingAttendeesAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
        .Field("editHeadlineAssignees")
        .Type<ListType<MeetingAttendeeChangeType>>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingAttendeesAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
        .UseProjection()
        .UseFiltering()
        .UseSorting();

        descriptor
        .Field("editHeadlineMeetings")
        .Type<ListType<MeetingMetadataChangeType>>()
        .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetPossibleMeetingsForUser(ct))
        .UseProjection()
        .UseFiltering()
        .UseSorting();
      }
      else
      {
        descriptor
          .Field("goals")
          .Type<ListType<GoalQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, GoalQueryModel>(async (keys, cancellationToken) => {
            var result = await ctx.Service<IDataContext>().GetGoalsForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_goals").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UsePaging<GoalQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("attendees")
          .Type<ListType<MeetingAttendeeQueryType>>()
          .Resolve(context =>
          {
            return context.GroupDataLoader<long, MeetingAttendeeQueryModel>(async (meetingIds, ct) =>
            {
              var result = await context.Service<IDataContext>().GetAttendeesByMeetingIdsAsync(meetingIds.ToList(), ct);
              return result.ToLookup(attendee => attendee.MeetingId);
            }).LoadAsync(context.Parent<MeetingQueryModel>().Id);
          })
          .UsePaging<MeetingAttendeeQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("attendeesLookup")
          .Type<ListType<MeetingAttendeeLookupQueryType>>()
          .Resolve(ctx => ctx.Parent<MeetingQueryModel>().AttendeesLookup)
          .UsePaging<MeetingAttendeeLookupQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("currentMeetingInstance")
          .Type<MeetingInstanceQueryType>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetInstanceForMeetingAsync(ctx.Parent<MeetingQueryModel>().CurrentMeetingInstanceId, ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection();
        // Deprecated TODO: remove it.
        descriptor
          .Field("createIssueAssignees")
          .Type<ListType<MeetingAttendeeQueryType>>()
          .Resolve(ctx => ctx.Parent<MeetingQueryModel>().Attendees)
          .UsePaging<MeetingAttendeeQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
          .UseProjection()
          .UseFiltering()
          .UseSorting();
        // Deprecated TODO: remove it.
        descriptor
          .Field("editIssueAssignees")
          .Type<ListType<MeetingAttendeeQueryType>>()
          .Resolve(ctx => ctx.Parent<MeetingQueryModel>().Attendees)
          .UsePaging<MeetingAttendeeQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
          .UseProjection()
          .UseFiltering()
          .UseSorting();
        // Deprecated TODO: remove it.
        descriptor
          .Field("createTodoAssignees")
          .Type<ListType<MeetingAttendeeQueryType>>()
          .Resolve(ctx => ctx.Parent<MeetingQueryModel>().Attendees)
          .UsePaging<MeetingAttendeeQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
          .UseProjection()
          .UseFiltering()
          .UseSorting();
        // Deprecated TODO: remove it.
        descriptor
          .Field("editTodoAssignees")
          .Type<ListType<MeetingAttendeeQueryType>>()
          .Resolve(ctx => ctx.Parent<MeetingQueryModel>().Attendees)
          .UsePaging<MeetingAttendeeQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        /* TODO: Merge this descriptor with the one below
        *  NOTE: This descriptor is used to allow ctx.Parent<MeetingModel>().MeetingPages to be populated in the following descriptor!
        *       Ideally we should be able to apply .Ignore() but I am unable to get that to work
        */
        descriptor
          .Field(t => t.MeetingPages).IsProjected(true)
          .Name("x")
          .Resolve(ctx => "x")
          //.Ignore()
          ;

        descriptor
          .Field(t => t.AttendeesLookup).IsProjected(true)
          .Name("y")
          .Resolve(ctx => "y")
          //.Ignore()
          ;

        descriptor
          .Field(t => t.Attendees).IsProjected(true)
          .Name("z")
          .Resolve(ctx => "z")
          //.Ignore()
          ;

        descriptor
          .Field("meetingPages")
          .Type<ListType<MeetingPageQueryType>>()
          .Resolve(ctx => ctx.Parent<MeetingQueryModel>().MeetingPages)
          .UsePaging<MeetingPageQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("meetingRatings")
          .Type<ListType<MeetingRatingType>>()
          //.Resolve(ctx => ctx.GroupDataLoader<long, MeetingRatingModel>(async (keys, cancellationToken) => {

          //  var service = ctx.Service<IDataContext>();
          //  var results =
          //      await
          //        keys
          //        .ToAsyncEnumerable()
          //        .SelectAwait(async key => new { Id = key, Value = await service.GetMeetingAttendeeInstancesForMeetingsAsync(new[] { key }, cancellationToken) })
          //        .ToLookupAsync(x => x.Id, x => x.Value, cancellationToken)
          //        ;

          //  return results;
          //}, "meeting_atttendeeInstances").LoadAsync(ctx.Parent<MeetingModel>().Id))
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMeetingAttendeeInstancesForMeetingsAsync(new[] { ctx.Parent<MeetingQueryModel>().Id }, cancellationToken))
          .UsePaging<MeetingRatingType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("notes")
          .Type<ListType<MeetingNoteQueryType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetNotesForMeetingsAsync(new[] { ctx.Parent<MeetingQueryModel>().Id }, cancellationToken))
          .UsePaging<MeetingNoteQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("metricDividers")
          .Type<ListType<MetricDividerType>>()
          .Resolve(async (ctx, ct) => (await ctx.Service<IDataContext>().GetMetricDividersForMeetingsAsync([ctx.Parent<MeetingQueryModel>().Id], ct)).Select(x => x.divider))
          .UsePaging<MetricDividerType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("todos")
          .Type<ListType<TodoQueryType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetTodosForMeetingsAsync(new[] { ctx.Parent<MeetingQueryModel>().Id }, cancellationToken))
          .UsePaging<TodoQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("todosActives")
          .Type<ListType<TodoQueryType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetTodosForMeetingsAsync(new[] { ctx.Parent<MeetingQueryModel>().Id }, cancellationToken, onlyActives: true))
          .UsePaging<TodoQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("issues")
          .Type<ListType<IssueQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, IssueQueryModel>(async (keys, cancellationToken) => {
            var result = await ctx.Service<IDataContext>().GetIssuesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_issues").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UsePaging<IssueQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("longTermIssues")
          .Type<ListType<IssueQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, IssueQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetLongTermIssuesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_longTermIssues").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UsePaging<IssueQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("sentToIssues")
          .Type<ListType<IssueQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, IssueQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetSentToIssuesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_sentToIssues").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UsePaging<IssueQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();


        descriptor
          .Field("archivedIssues")
          .Type<ListType<IssueQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, IssueQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetArchivedIssuesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_archivedIssues").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UsePaging<IssueQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("recentlySolvedIssues")
          .Type<ListType<IssueQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, IssueQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetRecentlySolvedIssuesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_recentlySolvedIssues").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UsePaging<IssueQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();


        descriptor
          .Field("headlines")
          .Type<ListType<HeadlineQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, HeadlineQueryModel>(async (keys, cancellationToken) => {
            var result = await ctx.Service<IDataContext>().GetHeadlinesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_headlines").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UsePaging<HeadlineQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("workspace")
          .Type<WorkspaceQueryType>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetWorkspaceForMeetingAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UseProjection();

        descriptor
          .Field("meetingInstances")
          .Type<ListType<MeetingInstanceQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MeetingInstanceQueryModel>(async (keys, cancellationToken) => {
            var result = await ctx.Service<IDataContext>().GetInstancesForMeetingsAsync(keys, cancellationToken);
            return result.ToLookup(x => x.RecurrenceId);
          }, "meeting_meetingInstances").LoadAsync(ctx.Parent<MeetingQueryModel>().Id))
          .UsePaging<MeetingInstanceQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("metrics")
          .Type<ListType<MetricQueryType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMetricsForMeetingAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UsePaging<MetricQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          //.UseFiltering<MetricFilterType<ListType<MeetingType>>>()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("weeklyMetricsLookup")
          .Type<ListType<MetricLookupQueryType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricsForMeetingLookupAsync(ctx.Parent<MeetingQueryModel>().Id, "WEEKLY", cancellationToken))
          .UsePaging<MetricLookupQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("monthlyMetricsLookup")
          .Type<ListType<MetricLookupQueryType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricsForMeetingLookupAsync(ctx.Parent<MeetingQueryModel>().Id, "MONTHLY", cancellationToken))
          .UsePaging<MetricLookupQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();
        descriptor
          .Field("quarterlyMetricsLookup")
          .Type<ListType<MetricLookupQueryType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricsForMeetingLookupAsync(ctx.Parent<MeetingQueryModel>().Id, "QUARTERLY", cancellationToken))
          .UsePaging<MetricLookupQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("issuesSentTo")
          .Type<ListType<IssueSentToType>>()
          // .Resolve(ctx =>
          //     ctx.GroupDataLoader<long, IssueSentToModel>(async (keys, cancellationToken) => {
          //         var result = await ctx.Service<IDataContext>().GetIssuesSentToForMeetingsAsync(keys, cancellationToken);
          //         return result.ToLookup(x => x.MeetingId);
          //     }).LoadAsync(ctx.Parent<MeetingModel>().Id))
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetIssuesSentToForMeetingAsync(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UsePaging<IssueSentToType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("metricAddExistingLookup")
          .Type<ListType<MetricAddExistingLookupQueryType>>()
          .Resolve((ctx, cancellationToken) =>ctx.Service<IDataContext>().GetMetricAddExistingLookup(cancellationToken, ctx.Parent<MeetingQueryModel>().Id))
          .UsePaging<MetricAddExistingLookupQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseSorting();

        descriptor
          .Field("metricsTabs")
          .Type<ListType<MetricTabType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricTabsForMeeting(ctx.Parent<MeetingQueryModel>().Id, cancellationToken))
          .UsePaging<MetricTabType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();
        // Deprecated TODO: remove it.
        descriptor
          .Field("createHeadlineAssignees")
          .Type<ListType<MeetingAttendeeQueryType>>()
          .Resolve(ctx => ctx.Parent<MeetingQueryModel>().Attendees)
          .UsePaging<MeetingAttendeeQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
          .UseProjection()
          .UseFiltering()
          .UseSorting();
        // Deprecated TODO: remove it.
        descriptor
        .Field("editHeadlineAssignees")
        .Type<ListType<MeetingAttendeeQueryType>>()
        .Resolve(ctx => ctx.Parent<MeetingQueryModel>().Attendees)
        .UsePaging<MeetingAttendeeQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
        .UseProjection()
        .UseFiltering()
        .UseSorting();

        descriptor
        .Field("editHeadlineMeetings")
        .Type<ListType<MeetingMetadataQueryType>>()
        .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetPossibleMeetingsForUser(ct))
        .UsePaging<MeetingMetadataQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
        .UseProjection()
        .UseFiltering()
        .UseSorting();
        // Deprecated TODO: remove it.
        descriptor
        .Field("createGoalAssignees")
        .Type<ListType<UserQueryType>>()
        .Resolve((ctx, cancellationToken) => ctx.Parent<MeetingQueryModel>().Attendees.Select(a => a.User))
        .UsePaging<UserQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting();
        // Deprecated TODO: remove it.
        descriptor
        .Field("editGoalAssignees")
        .Type<ListType<UserQueryType>>()
        .Resolve((ctx, cancellationToken) => ctx.Parent<MeetingQueryModel>().Attendees.Select(a => a.User))
        .UsePaging<UserQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting();

      }


      descriptor
        .Field("currentMeetingAttendee")
        .Type<MeetingAttendeeQueryType>()
        .Resolve(context => {
          var sw = Stopwatch.StartNew();
          //we don't want the pharent to try to initially load the attendees list (perfomance stuff)
          // so we make use of BatchDataLoader instead
          var res = context.BatchDataLoader<long, MeetingAttendeeQueryModel>(async (meetingIds, ct) => {
            var sw = Stopwatch.StartNew();
            var dc = context.Service<IDataContext>();
            var a = sw.ElapsedMilliseconds;
            var ids = meetingIds.ToList();
            var b = sw.ElapsedMilliseconds;
            var att = context.Parent<MeetingQueryModel>().Attendees;
            var result = await dc.GetCurrentAttendeesByMeetingIdsAsync(ids,
              context.Parent<MeetingQueryModel>().UserId, ct);

            var f = sw.ElapsedMilliseconds;
            var output = result.ToDictionary(attendee => attendee.MeetingId);
            var e = sw.ElapsedMilliseconds;
            return output;
          }).LoadAsync(context.Parent<MeetingQueryModel>().Id);
          var a = sw.ElapsedMilliseconds;
          return res;
        })
        //.Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetCallerAsMeetingAttendee(ctx.Parent<MeetingModel>().Id, cancellationToken))
        .UseProjection();

    }
  }
}