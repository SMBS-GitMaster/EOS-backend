using RadialReview.GraphQL.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    Task<IQueryable<CommentQueryModel>> GetCommentsAsync(RadialReview.Models.ParentType parentType, long parentId, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public async Task<IQueryable<CommentQueryModel>> GetCommentsAsync(RadialReview.Models.ParentType parentType, long parentId, CancellationToken cancellationToken)
    {
      return repository.GetComments(parentType, parentId, cancellationToken);
    }

  }

}