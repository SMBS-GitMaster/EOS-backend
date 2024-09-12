using HotChocolate.Types;
using RadialReview.BusinessPlan.Core.Repositories.Interfaces;
using RadialReview.BusinessPlan.Models.Utilities;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Organization;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.GraphQL
{
  public partial class MutationType
  {
    public void OrganizationSettingsMutations(IObjectTypeDescriptor descriptor)
    {
      descriptor
      .Field("setV3BusinessPlanId")
      .Argument("input", a => a.Type<SetV3BusinessPlanInputType>())
      .Authorize()
      .Resolve(async (ctx, cancellationToken) => {
        var input = ctx.ArgumentValue<SetV3BusinessPlanInput>("input");
        MutationResponse response = await ctx.Service<IBusinessPlanRepository>()
        .SetV3BusinessPlanIdInOrganizationSettings(input.BusinessPlanId);
        if (response.Status) return new GraphQLResponseBase(response.Status, response.Message);
        else return GraphQLResponseBase.Error(new ErrorDetail(response.Message, GraphQLErrorType.Validation));
      });

    }
    
  }
}
