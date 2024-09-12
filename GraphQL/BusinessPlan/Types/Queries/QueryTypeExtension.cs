using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using RadialReview.BusinessPlan.Core.Data.Models;
using RadialReview.BusinessPlan.Core.Repositories.Interfaces;
using RadialReview.GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.BusinessPlan.Types.Queries
{
  public class QueryTypeExtension
  {
    public static void BusinessPlanQueries(IObjectTypeDescriptor descriptor)
    {
      descriptor
        .Field("businessPlan")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<BusinessPlanQueryType>()
        .Resolve(ctx => ctx.Service<IBusinessPlanRepository>().GetById(ctx.ArgumentValue<long>("id")))
        .UseProjection()
        .Authorize()
        ;

      descriptor
        .Field("businessPlans")
        .Type<ListType<BusinessPlanQueryType>>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IBusinessPlanRepository>().GetAll())
        .UsePaging<BusinessPlanQueryType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        .Authorize()
        ;
    }
  }
}
