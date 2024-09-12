using HotChocolate.Types;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.Repositories;

namespace RadialReview.Core.GraphQL.Types.Mutations {

  public partial class UserCreateMutationType : InputObjectType<UserCreateModel> {
    protected override void Configure(IInputObjectTypeDescriptor<UserCreateModel> descriptor) {
      base.Configure(descriptor);
    }
  }
  public partial class UserEditMutationType : InputObjectType<UserEditModel> {
    protected override void Configure(IInputObjectTypeDescriptor<UserEditModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public class IncrementNumViewedNewFeaturesInput
  {
    public long UserId { get; set; }
  }

  public class IncrementNumViewedNewFeaturesOutput
  {
    public long UserId { get; set; }
    public int NumViewedNewFeatures { get; set; }
  }
}


namespace RadialReview.GraphQL {
  public partial class MutationType {
    public void AddUserMutations(IObjectTypeDescriptor descriptor) {

      descriptor
        .Field("CreateUser")
        .Argument("input", a => a.Type<NonNullType<UserCreateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateUser(ctx.ArgumentValue<UserCreateModel>("input")));

      descriptor
        .Field("EditUser")
        .Argument("input", a => a.Type<NonNullType<UserEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditUser(ctx.ArgumentValue<UserEditModel>("input")));

      descriptor
        .Field("IncrementNumViewedNewFeatures")
        .Argument("input", a => a.Type<NonNullType<InputObjectType<IncrementNumViewedNewFeaturesInput>>>())
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().IncrementNumViewedNewFeatures(ctx.ArgumentValue<IncrementNumViewedNewFeaturesInput>("input"), cancellationToken))
        .Authorize();
    }
  }
}
