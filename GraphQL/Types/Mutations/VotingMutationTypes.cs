using HotChocolate.Types;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.Repositories;

namespace RadialReview.Core.GraphQL.Types.Mutations {

  public partial class UpdateVotingStateMutationType : InputObjectType<VotingUpdateStateModel> {
    protected override void Configure(IInputObjectTypeDescriptor<VotingUpdateStateModel> descriptor) {
      base.Configure(descriptor);
    }
  }
  public partial class StartStarVotingMutationType : InputObjectType<StartStarVotingModel> {
    protected override void Configure(IInputObjectTypeDescriptor<StartStarVotingModel> descriptor) {
      base.Configure(descriptor);
    }
  }
  public partial class SubmitIssueStarVotesMutationType : InputObjectType<SubmitIssueStarVotesModel> {
    protected override void Configure(IInputObjectTypeDescriptor<SubmitIssueStarVotesModel> descriptor) {
      base.Configure(descriptor);
    }
  }
}


namespace RadialReview.GraphQL {
  public partial class MutationType {
    public void AddVotingMutations(IObjectTypeDescriptor descriptor) {

      descriptor
        .Field("UpdateVotingState")
        .Argument("input", a => a.Type<NonNullType<UpdateVotingStateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().UpdateVotingState(ctx.ArgumentValue<VotingUpdateStateModel>("input")));

      descriptor
        .Field("StartStarVoting")
        .Argument("input", a => a.Type<NonNullType<StartStarVotingMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().StartStarVoting(ctx.ArgumentValue<StartStarVotingModel>("input")));

      descriptor
        .Field("SubmitIssueStarVotes")
        .Argument("input", a => a.Type<NonNullType<SubmitIssueStarVotesMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().SubmitIssueStarVotes(ctx.ArgumentValue<SubmitIssueStarVotesModel>("input")));


    }
  }
}
