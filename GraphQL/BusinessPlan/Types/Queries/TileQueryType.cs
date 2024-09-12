using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using RadialReview.BusinessPlan.Core.Data.Models;
using RadialReview.BusinessPlan.Models;
using RadialReview.GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.BusinessPlan.Types.Queries
{
  public class TileQueryType: ObjectType<RadialReview.BusinessPlan.Models.BusinessPlanTile>
  {
    protected readonly bool isSubscription;
    public TileQueryType() : this(isSubscription: false) { }
    protected TileQueryType(bool isSubscription) { this.isSubscription = isSubscription; }

    protected override void Configure(IObjectTypeDescriptor<RadialReview.BusinessPlan.Models.BusinessPlanTile> descriptor)
    {
      base.Configure(descriptor);

      descriptor
       .Field("type")
       .Type<StringType>()
       .Resolve(ctx => "businessPlanTile")
       ;

      //NOTE: We aim to ignore the 'x' field using the Ignore() method.
      descriptor
       .Field(t => t.ListCollections).IsProjected(true)
       .Name("x")
       .Resolve(ctx => "x")
      //.Ignore()
      ;

      if (isSubscription)
      {
        descriptor
        .Field("listCollections")
        .Resolve(ctx => ctx.Parent<BusinessPlanTile>().ListCollections)
        .Type<ListType<BusinessPlanListCollectionChangeType>>()
        .UseProjection()
        .UseFiltering()
        .UseSorting();
      }
      else
      {
        descriptor
        .Field("listCollections")
        .Resolve(ctx => ctx.Parent<BusinessPlanTile>().ListCollections)
        .Type<ListType<CollectionQueryType>>()
        .UsePaging<CollectionQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting();
      }
    }
  }
  public class BusinessPlanTileChangeType : TileQueryType
  {
    public BusinessPlanTileChangeType() : base(true)
    {
    }
    protected override void Configure(IObjectTypeDescriptor<RadialReview.BusinessPlan.Models.BusinessPlanTile> descriptor)
    {
      base.Configure(descriptor);
      descriptor.Name("BusinessPlanTileModelChange");
    }

  }
}
