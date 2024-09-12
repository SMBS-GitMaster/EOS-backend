using HotChocolate.Types;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;

namespace RadialReview.GraphQL {

  public partial class MilestoneCreateMutationType : InputObjectType<MilestoneCreateModel> {
    protected override void Configure(IInputObjectTypeDescriptor<MilestoneCreateModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class MilestoneEditMutationType : InputObjectType<MilestoneEditModel> {
    protected override void Configure(IInputObjectTypeDescriptor<MilestoneEditModel> descriptor) {
      base.Configure(descriptor);
    }
  }
}

namespace RadialReview.GraphQL {
  public partial class MutationType {
    public void AddMilestoneMutations(IObjectTypeDescriptor descriptor) {

      descriptor
        .Field("CreateMilestone")
        .Argument("input", a => a.Type<NonNullType<MilestoneCreateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateMilestone(ctx.ArgumentValue<MilestoneCreateModel>("input")));

      descriptor
        .Field("EditMilestone")
        .Argument("input", a => a.Type<NonNullType<MilestoneEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditMilestone(ctx.ArgumentValue<MilestoneEditModel>("input")));

      descriptor
        .Field("DeleteMilestone")
        .Argument("milestoneId", a => a.Type<NonNullType<LongType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().DeleteMilestone(ctx.ArgumentValue<long>("milestoneId")));

    }
  }
}
