using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    #region Queries

    IQueryable<CommentQueryModel> GetComments(RadialReview.Models.ParentType parentType, long parentId, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<IdModel> CreateComment(CommentCreateModel commentCreateModel);

    Task<IdModel> DeleteComment(CommentDeleteModel commentDeleteModel);

    Task<IdModel> EditComment(CommentEditModel commentEditModel);

    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public IQueryable<CommentQueryModel> GetComments(RadialReview.Models.ParentType parentType, long parentId, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        return CommentAccessor.GetComments(caller, parentType, parentId).Select(x => RepositoryTransformers.TransformComment(x)).ToList();
      });
    }

    #endregion

    #region Mutations

    public async Task<IdModel> CreateComment(CommentCreateModel commentCreateModel)
    {
      Models.ParentType parentType;
      if (Enum.TryParse(commentCreateModel.CommentParentType, out parentType))
      {
        long id = await CommentAccessor.AddComment(caller, parentType, commentCreateModel.ParentId,
          commentCreateModel.Body, commentCreateModel.PostedTimestamp.FromUnixTimeStamp(),
          commentCreateModel.Author);
        return new IdModel(id);
      }

      return null;
    }

    public async Task<IdModel> EditComment(CommentEditModel commentEditModel)
    {
      Models.ParentType parentType;
      if (Enum.TryParse(commentEditModel.CommentParentType, out parentType))
      {
        long id = await CommentAccessor.EditComment(caller, commentEditModel.CommentId,
          parentType, commentEditModel.ParentId,
          commentEditModel.Body,
          commentEditModel.Author);
        return new IdModel(id);
      }

      return null;
    }

    public async Task<IdModel> DeleteComment(CommentDeleteModel commentDeleteModel)
    {
      Models.ParentType parentType;
      if (Enum.TryParse(commentDeleteModel.ParentType, out parentType))
      {
        long id = await CommentAccessor.DeleteComment(caller, commentDeleteModel.CommentId,
          parentType);
        return new IdModel(id);
      }

      return null;
    }

    #endregion

  }

}
