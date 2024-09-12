namespace RadialReview.GraphQL.Types
{
  using System.Collections.Generic;
  using System.Linq;
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;

  public class NodeCompletionStatsQueryType : ObjectType<NodeCompletionStatsQueryModel>
  {

    protected override void Configure(IObjectTypeDescriptor<NodeCompletionStatsQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "nodeCompletionStats");


      descriptor
        .Field(t => t.RecurrenceId == null ? t.UserId : t.RecurrenceId )
        .Type<LongType>()
        .IsProjected(true)
        ;

      descriptor
        .Field(t => t.StartDate)
        .Type<DateTimeType>()
        .IsProjected(true);

      descriptor
        .Field(t => t.EndDate)
        .Type<DateTimeType>()
        .IsProjected(true);

      descriptor
        .Field(t => t.GroupBy)
        .Type<StringType>()
        .IsProjected(true);

      descriptor
        .Field("todos")
        .Type<ListType<NodeStatDataQueryType>>()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetTodoStatsAsync(
          ctx.Parent<NodeCompletionStatsQueryModel>().RecurrenceId, ctx.Parent<NodeCompletionStatsQueryModel>().StartDate,
          ctx.Parent<NodeCompletionStatsQueryModel>().EndDate, ctx.Parent<NodeCompletionStatsQueryModel>().GroupBy,
          cancellationToken))
        .UseProjection()
        ;

      descriptor
        .Field("issues")
        .Type<ListType<NodeStatDataQueryType>>()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetIssueStatsAsync(
          ctx.Parent<NodeCompletionStatsQueryModel>().RecurrenceId, ctx.Parent<NodeCompletionStatsQueryModel>().StartDate,
          ctx.Parent<NodeCompletionStatsQueryModel>().EndDate, ctx.Parent<NodeCompletionStatsQueryModel>().GroupBy,
          cancellationToken))
        .UseProjection()
        ;

      descriptor
        .Field("milestones")
        .Type<ListType<NodeStatDataQueryType>>()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMilestoneStatsAsync(
          ctx.Parent<NodeCompletionStatsQueryModel>().RecurrenceId, ctx.Parent<NodeCompletionStatsQueryModel>().StartDate,
          ctx.Parent<NodeCompletionStatsQueryModel>().EndDate, ctx.Parent<NodeCompletionStatsQueryModel>().GroupBy,
          cancellationToken))
        .UseProjection()
        ;

      descriptor
        .Field("goals")
        .Type<ListType<NodeStatDataQueryType>>()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetGoalStatsAsync(
          ctx.Parent<NodeCompletionStatsQueryModel>().RecurrenceId, ctx.Parent<NodeCompletionStatsQueryModel>().StartDate,
          ctx.Parent<NodeCompletionStatsQueryModel>().EndDate, ctx.Parent<NodeCompletionStatsQueryModel>().GroupBy,
          cancellationToken))
        .UseProjection()
        ;

    }

  }
}