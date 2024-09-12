using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using HotChocolate.Data.Filters;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;

namespace RadialReview.GraphQL.Types
{
    public class MetricFilterType<TMeetingsType> : FilterInputType<MetricQueryModel>
  {
    protected override void Configure(IFilterInputTypeDescriptor<MetricQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Name("MetricFilterType_Advanced")
        .BindFieldsImplicitly()
        ;

      descriptor
        .Field(t => t.Meetings)
        .Name("meetings")
        .Type<FilterInputType<TMeetingsType>>()
        ;
    }
  }

  public class MetricChangeType : MetricQueryType
  {
    public MetricChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MetricQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MetricModelChange");
    }
  }

  public class MetricQueryType : ObjectType<MetricQueryModel>
  {
    protected readonly bool isSubscription;

    public MetricQueryType()
      : this(false)
    {
    }

    protected MetricQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MetricQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "metric");

      descriptor
          .Field(t => t.Id).IsProjected(true)
          .Type<LongType>();

      //NOTE: We want to ignore the following fields from '_a' to '_f'.
      descriptor
        .Field(t => t.ShowCumulative).IsProjected(true)
        .Name("_a")
        .Resolve(x => "_a")
        //.Ignore()
        ;
      ;

      descriptor
        .Field(t => t.CumulativeRange).IsProjected(true)
        .Name("_b")
        .Resolve(x => "_b")
        //.Ignore()
        ;

      descriptor
        .Field(t => t.ShowAverage).IsProjected(true)
        .Name("_c")
        .Resolve(x => "_c")
        //.Ignore()
        ;

      descriptor
        .Field(t => t.AverageRange).IsProjected(true)
        .Name("_d")
        .Resolve(x => "_d")
        //.Ignore()
        ;

      descriptor
        .Field(t => t.ProgressiveDate).IsProjected(true)
        .Name("_e")
        .Resolve(x => "_e")
        //.Ignore()
        ;

      descriptor
        .Field(t => t.StartOfWeek).IsProjected(true)
        .Name("_f")
        .Resolve(x => "_f")
        //.Ignore()
        ;

      descriptor
          .Field(t => t.RecurrenceId).IsProjected(true);

      descriptor
        .Field("metricData")
        .Type<MetricDataType>()
        .Resolve(async (ctx, cancellationToken) =>
        {
          var metric = ctx.Parent<MetricQueryModel>();
          var metricId = metric.Id;
          var recurrenceId = metric.RecurrenceId;

          var dataLoader = ctx.BatchDataLoader<long, MetricDataModel>(async (keys, ct) =>
          {
            var metricData = await ctx.Service<IDataContext>().GetMetricDataByMetricIds(keys, cancellationToken: ctx.RequestAborted, recurrenceId);

            return metricData.ToDictionary(x => x.Key, x => x.Value);
          });

          var result = await dataLoader.LoadAsync(metricId, ctx.RequestAborted);
          return result;
        })
        .UseProjection();

      descriptor
        .Field("metricDivider")
        .Type<MetricDividerType>()
        // .Resolve(ctx => ctx.GroupDataLoader<Tuple<long, long>, MetricDividerQueryModel>(async (keys, ct) =>
        // {
        //   var result = await ctx.Service<IDataContext>().GetMetricDividersAsync(keys, ct);
        //   return result.ToLookup(x => x.Item1, x => x.Item2);
        // }, "metric_metricDivider").LoadAsync(Tuple.Create(ctx.Parent<MetricQueryModel>().RecurrenceId.Value, ctx.Parent<MetricQueryModel>().Id)))
        .Resolve(async (ctx, ct) => ctx.Parent<MetricQueryModel>().RecurrenceId == null ? null : (await ctx.Service<IDataContext>().GetMetricDividersAsync([(recurrenceId: ctx.Parent<MetricQueryModel>().RecurrenceId.Value, measurableId: ctx.Parent<MetricQueryModel>().Id)], ct)).Select(x => x.divider).OrderBy(x => x.IndexInTable).LastOrDefault())
        .UseProjection()
        ;

      if (isSubscription)
      {
        descriptor
          .Field("customGoals")
          .Type<ListType<MetricCustomGoalChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MetricCustomGoalQueryModel>(async (keys, cancellationToken) =>
          {
            var results = await ctx.Service<IDataContext>().GetCustomGoalsForMetricsAsync(keys.ToList(), cancellationToken);
            return results.ToLookup(x => x.MeasurableId);
          }).LoadAsync(ctx.Parent<MetricQueryModel>().Id))
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
          }, "measurable_meetings").LoadAsync(ctx.Parent<MetricQueryModel>().Id))
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
          }, "measurable_scores").LoadAsync(ctx.Parent<MetricQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        // TODO: this resolver needs to be removed when FE transitions to the one above
        descriptor
          .Field("scores_nonPaginated")
          .Type<ListType<MetricScoreChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MetricScoreQueryModel>(async (keys, cancellationToken) =>
          {
            var results = await ctx.Service<IDataContext>().GetScoresForMeasurablesAsync(keys, cancellationToken);
            return results.ToLookup(x => x.MeasurableId);
          }, "measurable_scores").LoadAsync(ctx.Parent<MetricQueryModel>().Id))
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
          .Field(t => t.Meetings)
          .Name("meetings")
          .Type<ListType<MeetingQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MeetingQueryModel>(async (keys, cancellationToken) =>
          {
            var results = await ctx.Service<IDataContext>().GetMeetingsForMetricsAsync(keys, cancellationToken);
            return results.ToLookup(x => x.measurableId, x => x.meeting);
          }, "measurable_meetings").LoadAsync(ctx.Parent<MetricQueryModel>().Id))
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
          }, "measurable_scores").LoadAsync(ctx.Parent<MetricQueryModel>().Id))
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
          }, "measurable_scores").LoadAsync(ctx.Parent<MetricQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        // TODO: this resolver needs to be removed when FE transitions to the one above
        descriptor
          .Field("scores_nonPaginated")
          .Type<ListType<MetricScoreType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MetricScoreQueryModel>(async (keys, cancellationToken) =>
          {
            var results = await ctx.Service<IDataContext>().GetScoresForMeasurablesAsync(keys, cancellationToken);
            return results.ToLookup(x => x.MeasurableId);
          }, "measurable_scores").LoadAsync(ctx.Parent<MetricQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();
      }
    }
  }
}