using HotChocolate.Types;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;

namespace RadialReview.GraphQL;

public partial class MutationType
{
    protected static void AddMetricDividerMutations(IObjectTypeDescriptor descriptor)
    {
      descriptor
        .Field("CreateMetricDivider")
        .Argument("input", a => a.Type<NonNullType<InputObjectType<MetricDividerCreateModel>>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => {
          return await ctx.Service<IRadialReviewRepository>().CreateMetricDivider(ctx.ArgumentValue<MetricDividerCreateModel>("input"), cancellationToken);
        });
      ;

      descriptor
        .Field("EditMetricDivider")
        .Argument("input", a => a.Type<NonNullType<InputObjectType<MetricDividerEditModel>>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => {
          return await ctx.Service<IRadialReviewRepository>().EditMetricDivider(ctx.ArgumentValue<MetricDividerEditModel>("input"), cancellationToken);
        });
      ;

      descriptor
        .Field("DeleteMetricDivider")
        .Argument("input", a => a.Type<NonNullType<InputObjectType<MetricDividerDeleteModel>>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => {
          return await ctx.Service<IRadialReviewRepository>().DeleteMetricDivider(ctx.ArgumentValue<MetricDividerDeleteModel>("input"), cancellationToken);
        });
      ;

      descriptor
        .Field("SortAndReorderMetrics")
        .Argument("input", a => a.Type<NonNullType<InputObjectType<MetricDividerSortModel>>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => {
          await ctx.Service<IRadialReviewRepository>().SortAndReorderMetrics(ctx.ArgumentValue<MetricDividerSortModel>("input"), cancellationToken);
          return new VoidModel();
        });
      ;
    }
}