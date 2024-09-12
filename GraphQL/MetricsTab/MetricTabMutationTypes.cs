using HotChocolate.Types;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;


namespace RadialReview.Core.GraphQL.Types.Mutations {
  public class MetricTabCreateMutationType : InputObjectType<MetricTabCreateModel> {
    protected override void Configure(IInputObjectTypeDescriptor<MetricTabCreateModel> descriptor) {
      base.Configure(descriptor);
    }
  }
  public class MetricTabEditMutationType : InputObjectType<MetricTabEditModel> {
    protected override void Configure(IInputObjectTypeDescriptor<MetricTabEditModel> descriptor) {
      base.Configure(descriptor);
    }
  }
  public class MetricTabDeleteMutationType : InputObjectType<MetricTabDeleteModel> {
    protected override void Configure(IInputObjectTypeDescriptor<MetricTabDeleteModel> descriptor) {
      base.Configure(descriptor);
    }
  }
  public class MetricTabRemoveFromTabMutationType : InputObjectType<MetricRemoveFromTabModel> {
    protected override void Configure(IInputObjectTypeDescriptor<MetricRemoveFromTabModel> descriptor) {
      base.Configure(descriptor);
    }
  }
  public class MetricTabAddToTabMutationType : InputObjectType<MetricAddToTabModel> {
    protected override void Configure(IInputObjectTypeDescriptor<MetricAddToTabModel> descriptor) {
      base.Configure(descriptor);
    }
  }
  public class MetricTabRemoveAllMetricsMutationType : InputObjectType<MetricRemoveAllMetricsFromTabModel> {
    protected override void Configure(IInputObjectTypeDescriptor<MetricRemoveAllMetricsFromTabModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public class PinMetricTabMutationType : InputObjectType<PinMetricTabModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<PinMetricTabModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }
}

namespace RadialReview.GraphQL {
  public partial class MutationType {
    public void AddMetricsTabMutations(IObjectTypeDescriptor descriptor) {


      descriptor
        .Field("CreateMetricTab")
        .Argument("input", a => a.Type<NonNullType<MetricTabCreateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateMetricTab(ctx.ArgumentValue<MetricTabCreateModel>("input")));

      descriptor
        .Field("EditMetricTab")
        .Argument("input", a => a.Type<NonNullType<MetricTabEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditMetricTab(ctx.ArgumentValue<MetricTabEditModel>("input")));

      descriptor
        .Field("DeleteMetricTab")
        .Argument("input", a => a.Type<NonNullType<MetricTabDeleteMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().DeleteMetricTab(ctx.ArgumentValue<MetricTabDeleteModel>("input")));

      descriptor
        .Field("RemoveMetricFromTab")
        .Argument("input", a => a.Type<NonNullType<MetricTabRemoveFromTabMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().RemoveMetricFromTab(ctx.ArgumentValue<MetricRemoveFromTabModel>("input")));

      descriptor
        .Field("AddMetricToTab")
        .Argument("input", a => a.Type<NonNullType<MetricTabAddToTabMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().AddMetricToTab(ctx.ArgumentValue<MetricAddToTabModel>("input")));

      descriptor
        .Field("RemoveAllMetricsFromTab")
        .Argument("input", a => a.Type<NonNullType<MetricTabRemoveAllMetricsMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().RemoveAllMetricTabs(ctx.ArgumentValue<MetricRemoveAllMetricsFromTabModel>("input")));

      descriptor
        .Field("pinOrUnpinMetricTab")
        .Argument("input", a => a.Type<NonNullType<PinMetricTabMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().PinOrUnpinMetricTab(ctx.ArgumentValue<PinMetricTabModel>("input")));
    }
  }
}
