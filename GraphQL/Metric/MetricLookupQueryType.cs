using HotChocolate.Data.Filters;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.GraphQL.Types
{

  public class MetricLookupChangeType : MetricLookupQueryType
  {
    public MetricLookupChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MetricQueryModelLookup> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MetricLookupModelChange");
    }
  }

  public class MetricLookupQueryType : ObjectType<MetricQueryModelLookup>
  {
    protected readonly bool isSubscription;

    public MetricLookupQueryType()
      : this(false)
    {
    }

    protected MetricLookupQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MetricQueryModelLookup> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "metricLookup");

      descriptor
          .Field(t => t.Id).IsProjected(true)
          .Type<LongType>();

      if (isSubscription)
      {
        descriptor
          .Field("customGoals")
          .Type<ListType<MetricCustomGoalChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MetricCustomGoalQueryModel>(async (keys, cancellationToken) =>
          {
            var results = await ctx.Service<IDataContext>().GetCustomGoalsForMetricsAsync(keys.ToList(), cancellationToken);
            return results.ToLookup(x => x.MeasurableId);
          }).LoadAsync(ctx.Parent<MetricQueryModelLookup>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("meetings")
          .Type<ListType<MeetingChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MeetingQueryModel>(async (keys, cancellationToken) =>
          {
            var results = await ctx.Service<IDataContext>().GetMeetingsForMetricsAsync(keys, cancellationToken);
            return results.ToLookup(x => x.measurableId, x => x.meeting);
          }, "measurable_meetings").LoadAsync(ctx.Parent<MetricQueryModelLookup>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("scoresNonPaginated")
          .Type<ListType<MetricScoreChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MetricScoreQueryModel>(async (keys, cancellationToken) =>
          {
            var results = await ctx.Service<IDataContext>().GetScoresForMeasurablesAsync(keys, cancellationToken);
            return results.ToLookup(x => x.MeasurableId);
          }, "measurable_scores").LoadAsync(ctx.Parent<MetricQueryModelLookup>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();
      }
      else
      {
        descriptor
          .Field("customGoals")
          .Type<ListType<MetricCustomGoalType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MetricCustomGoalQueryModel>(async (keys, cancellationToken) =>
          {
            var results = await ctx.Service<IDataContext>().GetCustomGoalsForMetricsAsync(keys.ToList(), cancellationToken);
            return results.ToLookup(x => x.MeasurableId);
          }).LoadAsync(ctx.Parent<MetricQueryModel>().Id))
          .UsePaging<MetricCustomGoalType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("meetings")
          .Type<ListType<MeetingQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MeetingQueryModel>(async (keys, cancellationToken) =>
          {
            var results = await ctx.Service<IDataContext>().GetMeetingsForMetricsAsync(keys, cancellationToken);
            return results.ToLookup(x => x.measurableId, x => x.meeting);
          }, "measurable_meetings").LoadAsync(ctx.Parent<MetricQueryModelLookup>().Id))
          .UsePaging<MeetingQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("scores")
          .Type<ListType<MetricScoreType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MetricScoreQueryModel>(async (keys, cancellationToken) =>
          {
            var results = await ctx.Service<IDataContext>().GetScoresForMeasurablesAsync(keys, cancellationToken);
            return results.ToLookup(x => x.MeasurableId);
          }, "measurable_scores").LoadAsync(ctx.Parent<MetricQueryModelLookup>().Id))
          .UsePaging<MetricScoreType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("scoresNonPaginated")
          .Type<ListType<MetricScoreType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MetricScoreQueryModel>(async (keys, cancellationToken) =>
          {
            var results = await ctx.Service<IDataContext>().GetScoresForMeasurablesAsync(keys, cancellationToken);
            return results.ToLookup(x => x.MeasurableId);
          }, "measurable_scores").LoadAsync(ctx.Parent<MetricQueryModelLookup>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();
      }
    }
  }
}