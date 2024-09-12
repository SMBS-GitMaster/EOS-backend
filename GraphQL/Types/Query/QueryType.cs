namespace RadialReview.GraphQL
{
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.Repositories;
  using RadialReview.GraphQL.Types;
  using System.Linq;
  using RadialReview.Core.GraphQL.Common;
  using System;
  using RadialReview.GraphQL.Models;
  using RadialReview.Core.GraphQL.BusinessPlan.Types.Queries;
  using RadialReview.Core.GraphQL.Types;

  public class QueryType : ObjectType {
    protected override void Configure(IObjectTypeDescriptor descriptor) {
      base.Configure(descriptor);

      descriptor
        .Field("bloomLookupNode")
        .Argument("id", a => a.Type<NonNullType<StringType>>())
        .Type<BloomLookupType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetBloomLookupNodeAsync(ctx.ArgumentValue<string>("id"), cancellationToken))
        .Authorize()
        ;

      descriptor
        .Field("issue")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<SingleIssueType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetIssueByIdAsync(ctx.ArgumentValue<long>("id"), cancellationToken))
        .Authorize()
        ;

      descriptor
        .Field("user")
        .Argument("id", a => a.Type<NonNullType<LongType>>()) // It's important to accept as a string to not break FE
        .Type<UserQueryType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetUserByIdAsync(ctx.ArgumentValue<long>("id"), cancellationToken))
        .Authorize()
        ;

      descriptor
        .Field("nodeCompletionStats")
        .Argument("input", a => a.Type<NonNullType<NodeCompletionArgumentsQueryType>>())
        .Type<NodeCompletionStatsQueryType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetNodeCompletionStatsAsync(ctx.ArgumentValue<NodeCompletionArgumentsQueryModel>("input"), cancellationToken))
        .Authorize()
        ;

      descriptor
        .Field("users")
        .Type<ListType<UserQueryType>>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetUsersAsync(cancellationToken))
        .UsePaging<UserQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        .Authorize()
        ;

      descriptor
        .Field("workspaces")
        .Argument("userId", a => a.Type<NonNullType<LongType>>())
        .Type<ListType<WorkspaceQueryType>>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetWorkspacesAsync(ctx.ArgumentValue<long>("userId"), cancellationToken))
        .UsePaging<WorkspaceQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        .Authorize()
        ;

      descriptor
        .Field("workspace")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<WorkspaceQueryType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetWorkspaceAsync(ctx.ArgumentValue<long>("id"), cancellationToken))
        .UseProjection()
        .Authorize()
        ;

      descriptor
        .Field("meetingWorkspaceTile")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<WorkspaceTileQueryType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingWorkspaceTile(ctx.ArgumentValue<long>("id"), cancellationToken))
        .UseProjection()
        .Authorize()
        ;

      descriptor
        .Field("personalWorkspaceTile")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<WorkspaceTileQueryType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetPersonalWorkspaceTile(ctx.ArgumentValue<long>("id"), cancellationToken))
        .UseProjection()
        .Authorize()
        ;

      descriptor
        .Field("todos")
        .Argument("userId", a => a.Type<NonNullType<LongType>>())
        .Type<ListType<TodoQueryType>>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetTodosForUserAsync(ctx.ArgumentValue<long>("userId"), cancellationToken))
        .UsePaging<TodoQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        .Authorize()
        ;

      descriptor
        .Field("todo")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<TodoQueryType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetTodoByIdAsync(ctx.ArgumentValue<long>("id"), cancellationToken))
        .UseProjection()
        .Authorize()
        ;

      descriptor
        .Field("goals")
        .Argument("userId", a => a.Type<NonNullType<LongType>>())
        .Type<ListType<GoalQueryType>>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetGoalsForUserAsync(ctx.ArgumentValue<long>("userId"), cancellationToken))
        .UsePaging<GoalQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        .Authorize()
        ;

      descriptor
        .Field("goal")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<GoalQueryType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetGoalByIdAsync(ctx.ArgumentValue<long>("id"), cancellationToken))
        .UseProjection()
        .Authorize()
        ;

      descriptor
        .Field("meetings")
        .Type<ListType<MeetingQueryType>>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingsForUserAsync(cancellationToken))
        .UsePaging<MeetingQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        .Authorize();


      //!! Reinstate this later
      //descriptor
      //  .Field("meetings")
      //  .Argument("userId", a => a.Type<NonNullType<LongType>>())
      //  .Type<ListType<MeetingMetadataType>>()
      //  .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingMetadataForUsersAsync(new[] { ctx.ArgumentValue<long>("userId") }, cancellationToken))
      //  .UsePaging<MeetingMetadataType>(options: new PagingOptions { IncludeTotalCount = true })
      //  .UseProjection()
      //  .UseFiltering()
      //  .UseSorting()
      //  .Authorize();

      descriptor
        .Field("meetingSlow")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingQueryType>()
        .Resolve(async (ctx, cancellationToken) => {
          var res = await ctx.Service<IDataContext>().GetMeetingsAsync(new[] { ctx.ArgumentValue<long>("id") }, cancellationToken);
          return res.SingleOrDefault();
        }).Authorize();

      descriptor
        .Field("meeting")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingQueryType>()
        .Resolve(async (ctx, cancellationToken) => {
          var res = await ctx.Service<IDataContext>().GetFastMeetingsAsync(new[] { ctx.ArgumentValue<long>("id") }, cancellationToken);
          return res.SingleOrDefault();
        })
        .UseProjection()
        .Authorize();

      descriptor
        .Field("meetingModes")
        .Type<ListType<MeetingModeQueryType>>()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMeetingModes(cancellationToken))
        .UseProjection()
        .Authorize();

      descriptor
        .Field("metric")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MetricQueryType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMetricByIdAsync(ctx.ArgumentValue<long>("id"), cancellationToken))
        .UseProjection()
        .Authorize()
        ;

      descriptor
        .Field("metrics")
        .Type<ListType<MetricQueryType>>()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricsForUserAsync(cancellationToken))
        .UsePaging<MetricQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        .Authorize()
        ;


      descriptor
        .Field("metricsTab")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MetricTabType>()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricTabById(ctx.ArgumentValue<long>("id"), cancellationToken))
        .UseProjection()
        .Authorize()
        ;


      descriptor
          .Field("currentMeetingInstance")
          .Argument("meetingId", a => a.Type<NonNullType<LongType>>())
          .Type<MeetingInstanceQueryType>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetInstanceForMeetingAsync(ctx.ArgumentValue<long>("meetingId"), null, cancellationToken))
          .UseProjection()
          .Authorize()
          ;

      descriptor
        .Field("headlines")
        .Argument("userId", a => a.Type<NonNullType<LongType>>())
        .Type<ListType<HeadlineQueryType>>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetHeadlinesForUserAsync(ctx.ArgumentValue<long>("userId"), cancellationToken))
        .UsePaging<HeadlineQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        .Authorize()
        ;

      descriptor
        .Field("headline")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<HeadlineQueryType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetHeadlineByIdAsync(ctx.ArgumentValue<long>("id"), cancellationToken))
        .UseProjection()
        .Authorize()
        ;

      descriptor
        .Field("issueHistoryEntry")
        .Type<ListType<IssueHistoryEntryType>>()
        .Resolve(async (ctx, cancellationToken) =>
        {
          var results = await ctx.Service<IDataContext>().GetIssueHistoryEntriesAsync(cancellationToken);
          return results;
        })
        .UsePaging<IssueHistoryEntryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        .Authorize()
        ;

      descriptor
        .Field("note")
        .Argument("id", a => a.Type<NonNullType<StringType>>())
        .Type<PadNoteType>()
        .Resolve(ctx => new Models.PadNoteModel(){ Id = ctx.ArgumentValue<string>("id") })
        .UseProjection()
        .Authorize()
        ;

      descriptor
        .Field("metricMeetingsLookup")
        .Type<ListType<MetricMeetingLookupType>>()
        .UsePaging<MetricMeetingLookupType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseFiltering()
        .UseSorting()
        .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetMeetingsForUserByAdminStatus(ct));

      descriptor
        .Field("terms")
        .Type<TermsQueryType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetTermsAsync(cancellationToken))
        .UseProjection()
        .Authorize()
        ;

      descriptor
        .Field("currentTimeinUtcMS")
        .Type<LongType>()
        .Resolve(() =>
        {
          return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        })
        .Authorize();

      descriptor
        .Field("orgChart")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<OrgChartQueryType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IRadialReviewRepository>().GetOrgChartById(cancellationToken, ctx.ArgumentValue<long>("id")))
        .UseProjection()
        .Authorize()
        ;

      QueryTypeExtension.BusinessPlanQueries(descriptor);
    }
  }
}