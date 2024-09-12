using HotChocolate.Types;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;

namespace RadialReview.Core.GraphQL.Types.Mutations
{


  public partial class MetricCreateMutationType : InputObjectType<MetricCreateModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MetricCreateModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class MetricEditMutationType : InputObjectType<MetricEditModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MetricEditModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }
  public partial class MetricSortMutationType : InputObjectType<MetricSortModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MetricSortModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }
  public partial class MetricAddExistingToMeetingMutationType : InputObjectType<MetricAddExistingToMeetingModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MetricAddExistingToMeetingModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class MetricByMeetingIdMutationType : InputObjectType<MetricByMeetingIdModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MetricByMeetingIdModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }
}

namespace RadialReview.GraphQL
{
  public partial class MutationType
  {
    public void AddMetricsMutations(IObjectTypeDescriptor descriptor)
    {


      descriptor
        .Field("CreateMetric")
        .Argument("input", a => a.Type<NonNullType<MetricCreateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateMetric(ctx.ArgumentValue<MetricCreateModel>("input")));

      descriptor
        .Field("EditMetric")
        .Argument("input", a => a.Type<NonNullType<MetricEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditMetric(ctx.ArgumentValue<MetricEditModel>("input")));

      descriptor
        .Field("SortMetric")
        .Argument("input", a => a.Type<NonNullType<MetricSortMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().SortMetric(ctx.ArgumentValue<MetricSortModel>("input")));

      descriptor
        .Field("AddExistingMetricToMeeting")
        .Argument("input", a => a.Type<NonNullType<MetricAddExistingToMeetingMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().AddExistingMetricToMeeting(ctx.ArgumentValue<MetricAddExistingToMeetingModel>("input")));

      descriptor
        .Field("EditMetricMeetingIds")
        .Argument("input", a => a.Type<NonNullType<MetricByMeetingIdMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().updateMetricByMeetingIds(ctx.ArgumentValue<MetricByMeetingIdModel>("input")));
    }
  }
}
