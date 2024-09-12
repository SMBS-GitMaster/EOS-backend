namespace RadialReview.Core.GraphQL.Types;

using System.Linq;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Types;
using RadialReview.Repositories;

public class OrgChartSeatChangeType : OrgChartSeatQueryType
{
  public OrgChartSeatChangeType() : base(true)
  {
  }

  protected override void Configure(IObjectTypeDescriptor<OrgChartSeatQueryModel> descriptor)
  {
    base.Configure(descriptor);

    descriptor.Name("OrgChartSeatModelChange");
  }
}

public class OrgChartSeatQueryType : ObjectType<OrgChartSeatQueryModel>
{
  protected readonly bool isSubscription;

  protected OrgChartSeatQueryType(bool isSubscription)
  {
    this.isSubscription = isSubscription;
  }

  public OrgChartSeatQueryType()
    : this(false)
  {
  }

  protected override void Configure(IObjectTypeDescriptor<OrgChartSeatQueryModel> descriptor)
  {
    base.Configure(descriptor);

    descriptor
      .Field("type")
      .Resolve(_ => "orgChartSeat")
      ;

    if (isSubscription)
    {
      descriptor
       .Field("users")
       .Type<ListType<UserChangeType>>()
       .Resolve(async ctx => await ctx.GroupDataLoader<long, UserQueryModel>(async (keys, cancellationToken) =>
       {
         var query = ctx.Service<IRadialReviewRepository>().GetUsersForOrgChartSeats(keys, cancellationToken);
         var result = await query.ToListAsync(cancellationToken);
         return result.ToLookup(x => x.OrgChartSeatId, x => x.User);
       }, "orgchartseat_users").LoadAsync(ctx.Parent<OrgChartSeatQueryModel>().Id));

      descriptor
        .Field("directReports")
        .Type<ListType<OrgChartSeatChangeType>>()
        .Resolve((ctx, ct) => ctx.Service<IRadialReviewRepository>().GetDirectReportsForOrgChartSeats([ctx.Parent<OrgChartSeatQueryModel>().Id], ct).Select(x => x.OrgChartSeat).ToList())
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        ;

      descriptor
      .Field("position")
      .Type<PositionChangeType>()
      // .Resolve(_ => Enumerable.Empty<PositionQueryModel>())
      .Resolve((ctx, ct) => ctx.Service<IRadialReviewRepository>().GetPositionsForOrgChartSeats([ctx.Parent<OrgChartSeatQueryModel>().Id], ct).Select(
        x => x.Position
      ).FirstOrDefault())
      ;
    }
    else
    {
      descriptor
        .Field("users")
        .Type<ListType<UserQueryType>>()
        // .Resolve(_ => Enumerable.Empty<UserQueryModel>())
        .Resolve(async ctx => await ctx.GroupDataLoader<long, UserQueryModel>(async (keys, cancellationToken) =>
        {
          var query = ctx.Service<IRadialReviewRepository>().GetUsersForOrgChartSeats(keys, cancellationToken);
          var result = await query.ToListAsync(cancellationToken);
          return result.ToLookup(x => x.OrgChartSeatId, x => x.User);
        }, "orgchartseat_users").LoadAsync(ctx.Parent<OrgChartSeatQueryModel>().Id))
        .UsePaging<UserQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        // .UseProjection()
        // .UseFiltering()
        // .UseSorting()
        ;

      descriptor
        .Field("position")
        .Type<PositionQueryType>()
        // .Resolve(_ => Enumerable.Empty<PositionQueryModel>())
        .Resolve((ctx, ct) => ctx.Service<IRadialReviewRepository>().GetPositionsForOrgChartSeats([ctx.Parent<OrgChartSeatQueryModel>().Id], ct).Select(
          x => x.Position
        ).FirstOrDefault())
        ;

      descriptor
        .Field("directReports")
        .Type<ListType<OrgChartSeatQueryType>>()
        .Resolve((ctx, ct) => ctx.Service<IRadialReviewRepository>().GetDirectReportsForOrgChartSeats([ctx.Parent<OrgChartSeatQueryModel>().Id], ct).Select(x => x.OrgChartSeat).ToList())
        .UsePaging<OrgChartSeatQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        ;
    }
  }
}