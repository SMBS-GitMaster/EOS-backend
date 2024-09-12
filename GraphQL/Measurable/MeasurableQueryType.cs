namespace RadialReview.GraphQL.Types {
  using System.Linq;
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.GraphQL.Types;
  using RadialReview.Repositories;

  public class MeasurableChangeType : MeasurableQueryType  
  {
    public MeasurableChangeType()
      : base(isSubscription: true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MeasurableQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MeasurableModelChange");
    }
  }


  public class MeasurableQueryType : ObjectType<MeasurableQueryModel> {
    protected readonly bool isSubscription;  

    public MeasurableQueryType() 
      : this(isSubscription: false)
    {
    }

    protected MeasurableQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MeasurableQueryModel> descriptor) {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "measureable");


      descriptor
          .Field(t => t.Id)
          .Type<LongType>();
          
      if (isSubscription)
      {
        descriptor
            .Field("scores")
            .Type<ListType<MetricScoreChangeType>>()
            .Resolve(ctx => ctx.GroupDataLoader<long, MetricScoreQueryModel>(async (keys, cancellationToken) =>
            {
              var results = await ctx.Service<IDataContext>().GetScoresForMeasurablesAsync(keys, cancellationToken);
              return results.ToLookup(x => x.MeasurableId);
            }, "measurable_scores").LoadAsync(ctx.Parent<MeasurableQueryModel>().Id))
            .UseProjection()
            .UseFiltering()
            .UseSorting();
      }
      else 
      {
        descriptor
            .Field("scores")
            .Type<ListType<MetricScoreType>>()
            .Resolve(ctx => ctx.GroupDataLoader<long, MetricScoreQueryModel>(async (keys, cancellationToken) =>
            {
              var results = await ctx.Service<IDataContext>().GetScoresForMeasurablesAsync(keys, cancellationToken);
              return results.ToLookup(x => x.MeasurableId);
            }, "measurable_scores").LoadAsync(ctx.Parent<MeasurableQueryModel>().Id))
            .UsePaging<MetricScoreType>(options: new PagingOptions { IncludeTotalCount = true })
            .UseProjection()
            .UseFiltering()
            .UseSorting();

        //descriptor
        //    .Field("scores")
        //    .Type<ListType<MetricScoreType>>()
        //    .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetScoresForMeasurablesAsync(new[] { ctx.Parent<MeasurableModel>().Id }, cancellationToken))
        //    .UsePaging<MetricScoreType>(options: new PagingOptions { IncludeTotalCount = true })
        //    .UseProjection()
        //    .UseFiltering()
        //    .UseSorting();
      }
    }
  }
}