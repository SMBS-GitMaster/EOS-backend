using HotChocolate.Types;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;


namespace RadialReview.Core.GraphQL.Types.Mutations {
  public partial class FeedbackSubmitMutationType : InputObjectType<FeedbackSubmitModel> {
    protected override void Configure(IInputObjectTypeDescriptor<FeedbackSubmitModel> descriptor) {
      base.Configure(descriptor);
    }
  }
}


namespace RadialReview.GraphQL {
  public partial class MutationType {
    public void AddFeedbackMutations(IObjectTypeDescriptor descriptor) {

      descriptor
        .Field("SubmitFeedback")
        .Argument("input", a => a.Type<NonNullType<FeedbackSubmitMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().SubmitFeedback(ctx.ArgumentValue<FeedbackSubmitModel>("input")));


    }
  }
}
