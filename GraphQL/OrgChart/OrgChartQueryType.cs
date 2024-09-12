namespace RadialReview.Core.GraphQL.Types;

using System.Linq;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;

public class OrgChartChangeType : OrgChartQueryType
{
  public OrgChartChangeType() : base(true)
  {
  }

  protected override void Configure(IObjectTypeDescriptor<OrgChartQueryModel> descriptor)
  {
    base.Configure(descriptor);

    descriptor.Name("OrgChartModelChange");
  }
}

public class OrgChartQueryType : ObjectType<OrgChartQueryModel>
{
  protected readonly bool isSubscription;

  protected OrgChartQueryType(bool isSubscription)
  {
    this.isSubscription = isSubscription;
  }

  public OrgChartQueryType()
    : this(false)
  {
  }

  protected override void Configure(IObjectTypeDescriptor<OrgChartQueryModel> descriptor)
  {
    base.Configure(descriptor);

    descriptor
      .Field("type")
      .Resolve(_ => "orgChart")
      ;

    descriptor
      .Field(t => t.Id).IsProjected(true)
      .Type<LongType>()
      ;

    if (isSubscription)
    {
      descriptor
        .Field("seats")
        .Type<ListType<OrgChartSeatChangeType>>()
        .Resolve((ctx, ct) => ctx.Service<IRadialReviewRepository>().GetOrgChartSeatsForOrgCharts([ctx.Parent<OrgChartQueryModel>().Id], ct).Select(x => x.OrgChartSeat).ToList())
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        ;
    } else
    {
      descriptor
        .Field("seats")
        .Type<ListType<OrgChartSeatQueryType>>()
        // .Resolve(async ctx => await ctx.GroupDataLoader<long, OrgChartSeatQueryModel>(async (keys, cancellationToken) =>
        // {
        //   var query = ctx.Service<IRadialReviewRepository>().GetOrgChartSeatsForOrgCharts(keys, cancellationToken);
        //   var result = await query.ToListAsync(cancellationToken);
        //   return result.ToLookup(x => x.OrgChartId, x => x.OrgChartSeat);
        // }, "orgchart_orgchartseats").LoadAsync(ctx.Parent<OrgChartQueryModel>().Id))
        .Resolve((ctx, ct) => ctx.Service<IRadialReviewRepository>().GetOrgChartSeatsForOrgCharts([ctx.Parent<OrgChartQueryModel>().Id], ct).Select(x => x.OrgChartSeat).ToList())
        .UsePaging<OrgChartSeatQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        ;
    }
  }
}