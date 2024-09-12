using HotChocolate.Types;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;

namespace RadialReview.GraphQL {
  public partial class MutationType {
    public void AddIframeEmbedMutations(IObjectTypeDescriptor descriptor) {
      descriptor
        .Field("IframeEmbedCheck")
        .Argument("url", a => a.Type<StringType>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => ctx.Service<IRadialReviewRepository>().IframeEmbedCheck(ctx.ArgumentValue<string>("url")));
    }
  }
}
