namespace RadialReview.Core.GraphQL.Types;

using System.Linq;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using RadialReview.Core.GraphQL.Models;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Types;
using RadialReview.Repositories;

public class PositionChangeType : PositionQueryType
{
  public PositionChangeType() : base(true)
  {
  }

  protected override void Configure(IObjectTypeDescriptor<OrgChartPositionQueryModel> descriptor)
  {
    base.Configure(descriptor);

    descriptor.Name("PositionModelChange");
  }
}

public class PositionQueryType : ObjectType<OrgChartPositionQueryModel>
{
  protected readonly bool isSubscription;

  protected PositionQueryType(bool isSubscription)
  {
    this.isSubscription = isSubscription;
  }

  public PositionQueryType()
    : this(false)
  {
  }

  protected override void Configure(IObjectTypeDescriptor<OrgChartPositionQueryModel> descriptor)
  {
    base.Configure(descriptor);

    descriptor
      .Field("type")
      .Resolve(_ => "orgChartPosition")
      ;
    if(isSubscription)
    {
      descriptor
        .Field("roles")
        .Type<ListType<PositionRoleChangeType>>()
        .Resolve((ctx, ct) => ctx.Service<IRadialReviewRepository>().GetPositionRoleForOrgChartSeat(ctx.Parent<OrgChartPositionQueryModel>().Id, ct));
    } else
    {
      descriptor
        .Field("roles")
        .Type<ListType<PositionRoleQueryType>>()
        .Resolve((ctx, ct) => ctx.Service<IRadialReviewRepository>().GetPositionRoleForOrgChartSeat(ctx.Parent<OrgChartPositionQueryModel>().Id, ct));
    }
  }
}