using HotChocolate.Types;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;

namespace RadialReview.GraphQL
{

  public partial class FavoriteCreateMutationType : InputObjectType<FavoriteCreateMutationModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<FavoriteCreateMutationModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class FavoriteEditMutationType : InputObjectType<FavoriteEditMutationModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<FavoriteEditMutationModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }
  public partial class FavoriteDeleteMutationType : InputObjectType<FavoriteDeleteMutationModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<FavoriteDeleteMutationModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }
}

namespace RadialReview.GraphQL
{
  public partial class MutationType
  {
    public void AddFavoriteMutations(IObjectTypeDescriptor descriptor)
    {

      descriptor
        .Field("CreateFavorite")
        .Argument("input", a => a.Type<NonNullType<FavoriteCreateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateFavorite(ctx.ArgumentValue<FavoriteCreateMutationModel>("input")));

      descriptor
        .Field("EditFavorite")
        .Argument("input", a => a.Type<NonNullType<FavoriteEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditFavorite(ctx.ArgumentValue<FavoriteEditMutationModel>("input")));

      descriptor
        .Field("DeleteFavorite")
        .Argument("input", a => a.Type<NonNullType<FavoriteDeleteMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().DeleteFavorite(ctx.ArgumentValue<FavoriteDeleteMutationModel>("input")));

    }
  }
}
