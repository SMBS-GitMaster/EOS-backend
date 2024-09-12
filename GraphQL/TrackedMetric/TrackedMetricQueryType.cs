namespace RadialReview.GraphQL.Types
{
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;
  using System.Linq;

  public class TrackedMetricChangeType : TrackedMetricQueryType
  {
    public TrackedMetricChangeType() 
      : base(isSubscription: true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<TrackedMetricQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("TrackedMetricModelChange");
    }
  }

  public class TrackedMetricQueryType : ObjectType<TrackedMetricQueryModel>
  {
    protected readonly bool isSubscription;
    
    public TrackedMetricQueryType() 
      : this(isSubscription: false)
    {
    }

    protected TrackedMetricQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<TrackedMetricQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "trackedMetric")
          ;

      descriptor
          .Field(t => t.Id)
          .Type<LongType>()
          .IsProjected(true);

      descriptor
          .Field(t => t.UserId)
          .Type<LongType>()
          .IsProjected(true);

      descriptor
          .Field(t => t.MetricId)
          .Type<LongType>()
          .IsProjected(true);

      if (isSubscription)
      {
        descriptor
          .Field("metric")
          .Type<MetricChangeType>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricById(ctx.Parent<TrackedMetricQueryModel>().MetricId, cancellationToken))
          .UseProjection()
          .UseFiltering()
          ;

        descriptor
          .Field("creator")
          .Type<UserChangeType>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetUserByIdAsync(ctx.Parent<TrackedMetricQueryModel>().UserId, cancellationToken))
          .UseProjection()
          .UseFiltering()
          ;
      }
      else 
      {
        descriptor
          .Field("metric")
          .Type<MetricQueryType>()
          .Resolve(context =>  context.BatchDataLoader<long, MetricQueryModel>(async (keys, ct) => {
              var result = await context.Service<IDataContext>().GetMetricsByIds(keys.ToList(), ct);
              return result.ToDictionary(metric => metric.Id);
            }).LoadAsync(context.Parent<TrackedMetricQueryModel>().MetricId)
          )
          .UseProjection()
          .UseFiltering()
          ;

        descriptor
          .Field("creator")
          .Type<UserQueryType>()
          .Resolve(context => context.BatchDataLoader<long, UserQueryModel>(async (keys, ct) => {
            var result = await context.Service<IDataContext>().GetTrackedMetricCreators(keys.ToList(), ct);
            return result.ToDictionary(user => user.Id);
          }).LoadAsync(context.Parent<TrackedMetricQueryModel>().UserId)
          )
          .UseProjection()
          .UseFiltering()
          ;
      }
    }
  }
}
