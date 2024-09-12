using HotChocolate.Types;
using RadialReview.Core.GraphQL.OrgChart.OrgChartSeat;
using RadialReview.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.GraphQL
{
  public partial class OrgChartSeatMutationType : InputObjectType<OrgChartSeatEditModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<OrgChartSeatEditModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class OrgChartSeatRoleMutationType : InputObjectType<OrgChartSeatEditRoleModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<OrgChartSeatEditRoleModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class OrgChartSeatCreateRoleMutationType : InputObjectType<OrgChartSeatCreateRoleModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<OrgChartSeatCreateRoleModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class OrgChartSeatDeleteRoleMutationType : InputObjectType<OrgChartSeatDeleteRoleModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<OrgChartSeatDeleteRoleModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class CreateOrgChartSeatMutationType : InputObjectType<OrgChartSeatCreateModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<OrgChartSeatCreateModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class DeleteOrgChartSeatMutationType : InputObjectType<OrgChartSeatDeleteModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<OrgChartSeatDeleteModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class MutationType
  {
    public void AddOrgChartSeatMutations(IObjectTypeDescriptor descriptor)
    {
      descriptor
        .Field("EditOrgChartSeat")
        .Argument("input", a => a.Type<NonNullType<OrgChartSeatMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditOrgChartSeat(ctx.ArgumentValue<OrgChartSeatEditModel>("input")));

      descriptor
        .Field("CreateOrgChartSeat")
        .Argument("input", a => a.Type<NonNullType<CreateOrgChartSeatMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateOrgChartSeat(ctx.ArgumentValue<OrgChartSeatCreateModel>("input")));

      descriptor
        .Field("EditRoleOrgChartSeat")
        .Argument("input", a => a.Type<NonNullType<OrgChartSeatRoleMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditRoleChartSeat(ctx.ArgumentValue<OrgChartSeatEditRoleModel>("input")));

      descriptor
        .Field("CreateOrgChartPositionRole")
        .Argument("input", a => a.Type<NonNullType<OrgChartSeatCreateRoleMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cc) => await ctx.Service<IRadialReviewRepository>().CreateRoleChartSeat(ctx.ArgumentValue<OrgChartSeatCreateRoleModel>("input")));

      descriptor
        .Field("DeleteOrgChartPositionRole")
        .Argument("input", a => a.Type<NonNullType<OrgChartSeatDeleteRoleMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cc) => await ctx.Service<IRadialReviewRepository>().DeleteRoleChartSeat(ctx.ArgumentValue<OrgChartSeatDeleteRoleModel>("input")));

      descriptor
        .Field("DeleteOrgChartSeat")
        .Argument("input", a => a.Type<NonNullType<DeleteOrgChartSeatMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cc) => await ctx.Service<IRadialReviewRepository>().DeleteOrgChartSeat(ctx.ArgumentValue<OrgChartSeatDeleteModel>("input")));
    }
  }
}