using HotChocolate.Types;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Types.Mutations
{
  public partial class MetricScoreCreateMutationType : InputObjectType<MetricScoreCreateModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MetricScoreCreateModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class MetricScoreEditMutationType : InputObjectType<MetricScoreEditModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MetricScoreEditModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

}

namespace RadialReview.GraphQL
{
  public partial class MutationType
  {
    public void AddMetricScoreMutations(IObjectTypeDescriptor descriptor)
    {
      descriptor
        .Field("CreateMetricScore")
        .Argument("input", a => a.Type<NonNullType<MetricScoreCreateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateMetricScore(ctx.ArgumentValue<MetricScoreCreateModel>("input")));

      descriptor
        .Field("EditMetricScore")
        .Argument("input", a => a.Type<NonNullType<MetricScoreEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditMetricScore(ctx.ArgumentValue<MetricScoreEditModel>("input")));

    }
  }
}
