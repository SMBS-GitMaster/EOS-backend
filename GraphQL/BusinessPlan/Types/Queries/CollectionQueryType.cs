using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using RadialReview.BusinessPlan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.BusinessPlan.Types.Queries
{
  public class CollectionQueryType : ObjectType<BusinessPlanListCollection>
  {
    protected readonly bool isSubscription;
    public CollectionQueryType() : this(isSubscription: false) { }
    protected CollectionQueryType(bool isSubscription) { this.isSubscription = isSubscription; }

    protected override void Configure(IObjectTypeDescriptor<BusinessPlanListCollection> descriptor)
    {
      base.Configure(descriptor);

      descriptor
       .Field("type")
       .Type<StringType>()
       .Resolve(ctx => "businessPlanListCollection")
       ;

      //NOTE: We aim to ignore the 'x' field using the Ignore() method.
      descriptor
       .Field(t => t.ListItems).IsProjected(true)
       .Name("x")
       .Resolve(ctx => "x")
      //.Ignore()
      ;

      if (isSubscription)
      {
        descriptor
        .Field("listItems")
        .Type<ListType<BusinessPlanItemChangeType>>()
        .Resolve(ctx => ctx.Parent<BusinessPlanListCollection>().ListItems)
        .UseProjection()
        .UseFiltering()
        .UseSorting();
      }
      else
      {
        descriptor
        .Field("listItems")
        .Type<ListType<ItemQueryType>>()
        .Resolve(ctx => ctx.Parent<BusinessPlanListCollection>().ListItems)
        .UsePaging<ItemQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting();
      }

    
    }
  }

  public class BusinessPlanListCollectionChangeType : CollectionQueryType
  {
    public BusinessPlanListCollectionChangeType() : base(true)
    {
    }
    protected override void Configure(IObjectTypeDescriptor<BusinessPlanListCollection> descriptor)
    {
      base.Configure(descriptor);
      descriptor.Name("BusinessPlanListCollectionModelChange");
    }

  }
}
