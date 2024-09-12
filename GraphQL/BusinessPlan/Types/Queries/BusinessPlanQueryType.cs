using AutoMapper.Configuration.Conventions;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using RadialReview.BusinessPlan.Core.Data.Models;
using RadialReview.BusinessPlan.Models;
using RadialReview.Exceptions;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.BusinessPlan.Types.Queries
{
  public class BusinessPlanQueryType : ObjectType<BusinessPlanModel>
  {
    protected readonly bool isSubscription;
    public BusinessPlanQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<BusinessPlanModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
       .Field("type")
       .Type<StringType>()
       .Resolve(ctx => "businessPlan")
       ;

      descriptor
          .Field(t => t.MeetingId)
          .Type<LongType>()
          .IsProjected(true) // using projected true to make functional ListType sub node when the field is not included in the query
          ;

      descriptor
          .Field(t => t.JsonData)
          .Name("x")
          .Resolve(ctx => "x")
          .IsProjected(true)
          ;

      descriptor
          .Field(t => t.IsShared)
          .Type<BooleanType>()
          ;

      descriptor.Field(t => t.DateLastModified)
      .Resolve(r =>
      {
        DateTime date = r.Parent<BusinessPlanModel>().DateLastModified;
        return date.ToUnixTimeStamp();
      }).Type<NonNullType<FloatType>>();

      // If we need to resolve a different subscription response to the query, add a Validation statement using the isSubscription property.
      descriptor
        .Field("currentUserPermissions")
        .Type<ObjectType<MeetingPermissionsModel>>()
        .Resolve(async (ctx, ct) => {
          // try catch to return user when user does not have access to meeting
          try
          {
            return await ctx.Service<IDataContext>().GetPermissionsForCallerOnMeetingAsync(ctx.Parent<BusinessPlanModel>().MeetingId);
          }
          catch (PermissionsException ex)
          {
            return null;
          }
        })
        .UseProjection()
        ;

      if (isSubscription)
      {
        descriptor
        .Field("bpTiles")
        .Resolve(ctx => ctx.Parent<BusinessPlanModel>().JsonData)
        .Type<ListType<BusinessPlanTileChangeType>>()
        .UseProjection()
        .UseFiltering()
        .UseSorting();
      }
      else
      {
        descriptor
        .Field("bpTiles")
        .Resolve(ctx => ctx.Parent<BusinessPlanModel>().JsonData)
        .Type<ListType<TileQueryType>>()
        .UsePaging<TileQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting();
      }
    }
  }

  public class BusinessPlanChangeType : BusinessPlanQueryType
  {
    public BusinessPlanChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<BusinessPlanModel> descriptor)
    {
      base.Configure(descriptor);
      descriptor.Name("BusinessPlanModelChange");
    }
  }
}

