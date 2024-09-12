namespace RadialReview.Repositories
{
  using RadialReview.Accessors;
  using RadialReview.Core.GraphQL.Models.Mutations;
  using RadialReview.Core.Repositories;
  using RadialReview.GraphQL.Models;
  using RadialReview.GraphQL.Models.Mutations;
  using RadialReview.Models.L10;
  using RadialReview.Utilities;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;

  public partial interface IRadialReviewRepository
  {

    Task<IdModel> CreateFavorite(FavoriteCreateMutationModel FavoriteCreateModel);
    Task<IdModel> EditFavorite(FavoriteEditMutationModel FavoriteEditModel);
    Task<IdModel> DeleteFavorite(FavoriteDeleteMutationModel FavoriteDeleteModel);

  }

  public partial class RadialReviewRepository
  {


    public async Task<IdModel> CreateFavorite(FavoriteCreateMutationModel FavoriteCreateModel)
    {
      Models.FavoriteType parentType;
      if (Enum.TryParse(FavoriteCreateModel.ParentType, out parentType))
      {
        long id = await FavoriteAccessor.AddFavorite(caller, FavoriteCreateModel.User, parentType,
          FavoriteCreateModel.ParentId, FavoriteCreateModel.Position,
          FavoriteCreateModel.PostedTimestamp.FromUnixTimeStamp());
        return new IdModel(id);
      }

      return null;
    }

    public async Task<IdModel> EditFavorite(FavoriteEditMutationModel FavoriteEditModel)
    {
      Models.FavoriteType parentType;      
      bool parentTypeParsed = Enum.TryParse(FavoriteEditModel.ParentType, out parentType);   

      long id = await FavoriteAccessor.EditFavorite(caller, FavoriteEditModel.FavoriteId,
          parentTypeParsed ? parentType : null, FavoriteEditModel.ParentId,
          FavoriteEditModel.Position,
          FavoriteEditModel.User);
      return new IdModel(id);

    }

    public async Task<IdModel> DeleteFavorite(FavoriteDeleteMutationModel FavoriteDeleteModel)
    {
      Models.FavoriteType parentType;
      if (Enum.TryParse(FavoriteDeleteModel.ParentType, out parentType))
      {
        long id = await FavoriteAccessor.DeleteFavorite(caller, FavoriteDeleteModel.FavoriteId,
          parentType);
        return new IdModel(id);
      }

      return null;
    }

  }


}
