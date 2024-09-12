using HotChocolate.Data.Filters;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using RadialReview.Core.GraphQL.MetricFormulaLookup;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Types;
using RadialReview.Repositories;
using System.Linq;

namespace RadialReview.Core.GraphQL.MetricAddExistingLookup
{
  public class MetricAddExistingLookupFilterType<TMeetingsType> : FilterInputType<MetricAddExistingLookupQueryModel>
  {
    protected override void Configure(IFilterInputTypeDescriptor<MetricAddExistingLookupQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Name("MetricAddExistingLookupFilterType_Advanced")
        .BindFieldsImplicitly()
        ;

      descriptor
        .Field(t => t.Meetings)
        .Name("meetings")
        .Type<FilterInputType<TMeetingsType>>()
        ;
    }
  }
  public class MetricAddExistingLookupChangeType : MetricAddExistingLookupQueryType
  {
    public MetricAddExistingLookupChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MetricAddExistingLookupQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MetricAddExistingLookupModelChange");
    }
  }

  public class MetricAddExistingLookupQueryType : ObjectType<MetricAddExistingLookupQueryModel>
  {
    protected readonly bool isSubscription;

    public MetricAddExistingLookupQueryType()
      : this(false)
    {
    }

    protected MetricAddExistingLookupQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }
    protected override void Configure(IObjectTypeDescriptor<MetricAddExistingLookupQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "metricAddExistingLookup");

      descriptor
        .Field(t => t.Id)
        .Type<LongType>()
        .IsProjected(true);

      if (isSubscription)
      {
        descriptor
         .Field(t => t.Meetings)
         .Name("meetings")
         .Type<ListType<MeetingChangeType>>()
         .Resolve(ctx => ctx.GroupDataLoader<long, MeetingQueryModel>(async (keys, cancellationToken) =>
         {
           var results = await ctx.Service<IDataContext>().GetMeetingsForMetrics(keys, cancellationToken);
           return results.ToLookup(x => x.measurableId, x => x.meeting);
         }, "metricAddExistingLookup_meetings").LoadAsync(ctx.Parent<MetricAddExistingLookupQueryModel>().Id))
         .UseProjection()
         .UseFiltering()
         .UseSorting();
      }
      else
      {
        descriptor
           .Field(t => t.Meetings)
           .Name("meetings")
           .Type<ListType<MeetingQueryType>>()
           .Resolve(ctx => ctx.GroupDataLoader<long, MeetingQueryModel>(async (keys, cancellationToken) =>
           {
             var results = await ctx.Service<IDataContext>().GetMeetingsForMetrics(keys, cancellationToken);
             return results.ToLookup(x => x.measurableId, x => x.meeting);
           }, "metricAddExistingLookup_meetings").LoadAsync(ctx.Parent<MetricAddExistingLookupQueryModel>().Id))
           .UsePaging<MeetingQueryType>(options: new PagingOptions { IncludeTotalCount = true })
           .UseProjection()
           .UseFiltering()
           .UseSorting();

      }
    }

  }
}
