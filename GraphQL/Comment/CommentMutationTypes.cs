using HotChocolate.Types;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;

namespace RadialReview.GraphQL {

  public partial class CommentCreateMutationType : InputObjectType<CommentCreateModel> {
    protected override void Configure(IInputObjectTypeDescriptor<CommentCreateModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class CommentEditMutationType : InputObjectType<CommentEditModel> {
    protected override void Configure(IInputObjectTypeDescriptor<CommentEditModel> descriptor) {
      base.Configure(descriptor);
    }
  }
  public partial class CommentDeleteMutationType : InputObjectType<CommentDeleteModel> {
    protected override void Configure(IInputObjectTypeDescriptor<CommentDeleteModel> descriptor) {
      base.Configure(descriptor);
    }
  }
}

namespace RadialReview.GraphQL {
  public partial class MutationType {
    public void AddCommentMutations(IObjectTypeDescriptor descriptor) {

      descriptor
        .Field("CreateComment")
        .Argument("input", a => a.Type<NonNullType<CommentCreateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateComment(ctx.ArgumentValue<CommentCreateModel>("input")));

      descriptor
        .Field("EditComment")
        .Argument("input", a => a.Type<NonNullType<CommentEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditComment(ctx.ArgumentValue<CommentEditModel>("input")));

      descriptor
        .Field("DeleteComment")
        .Argument("input", a => a.Type<NonNullType<CommentDeleteMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().DeleteComment(ctx.ArgumentValue<CommentDeleteModel>("input")));

    }
  }
}
