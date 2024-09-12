namespace RadialReview.GraphQL.Types
{
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;
  using System.Linq;

  public class MetricTabChangeType : MetricTabType
  {
    public MetricTabChangeType()
      : base(isSubscription: true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MetricsTabQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MetricTabModelChange");
    }
  }

  public class MetricTabType : ObjectType<MetricsTabQueryModel>
  {
    protected readonly bool isSubscription;

    public MetricTabType()
      : this(isSubscription: false)
    {
    }

    protected MetricTabType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MetricsTabQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "metricsTab")
          ;

      descriptor
          .Field(t => t.Id)
          .Type<LongType>()
          .IsProjected(true);

      descriptor
          .Field(t => t.MeetingId)
          .Type<LongType>()
          .IsProjected(true);

      descriptor
          .Field(t => t.UserId)
          .Type<LongType>()
          .IsProjected(true);

      descriptor
        .Field("creator")
        .Type<UserQueryType>()
        .Resolve(context => context.BatchDataLoader<long, UserQueryModel>(async (keys, ct) => {
          var result = await context.Service<IDataContext>().GetTrackedMetricCreators(keys.ToList(), ct);
          return result.ToDictionary(user => user.Id);
        }).LoadAsync(context.Parent<MetricsTabQueryModel>().UserId))
        .UseProjection()
        .UseFiltering()
        ;

      descriptor
        .Field("meeting")
        .Type<MeetingQueryType>()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMeetingAsync(ctx.Parent<MetricsTabQueryModel>().MeetingId, LoadMeetingModel.False(), cancellationToken))
        .UseProjection()
        .UseFiltering()
      ;

      descriptor
        .Field("isPinnedToTabBar")
        .Type<NonNullType<BooleanType>>()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricTabPinned(ctx.Parent<MetricsTabQueryModel>().Id, cancellationToken))
        ;

      if (isSubscription)
      {
        descriptor
          .Field("trackedMetrics")
          .Type<ListType<TrackedMetricChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetTrackedMetricsForTab(ctx.Parent<MetricsTabQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();
          ;
      }
      else 
      {
        descriptor
          .Field("trackedMetrics")
          .Type<ListType<TrackedMetricQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, TrackedMetricQueryModel>(async (keys, cancellationToken) =>
          {
            var results = await ctx.Service<IDataContext>().GetTrackedMetricsForTab(keys.ToList(), cancellationToken);
            return results.ToLookup(x => x.MetricTabId);
          }).LoadAsync(ctx.Parent<MetricsTabQueryModel>().Id))
          .UsePaging<TrackedMetricQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();
          ;
      }
    }
  }
}
