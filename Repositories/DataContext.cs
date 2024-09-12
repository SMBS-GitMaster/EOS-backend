using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial class DataContext : IDataContext
  {

    #region Members

    protected readonly IRadialReviewRepository repository;

    #endregion

    #region Constructor
    public DataContext(IRadialReviewRepository repository)
    {
      this.repository = repository;
    }

    #endregion

    #region Public Methods

    public async Task<IQueryable<UserOrganizationQueryModel>> GetOrganizationForUserAsync(long userId, CancellationToken cancellationToken)
    {
      return repository.GetUserOrganizationForUsers(new[] { userId }, cancellationToken);
    }

    public async Task<IQueryable<UserOrganizationQueryModel>> GetOrganizationForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      return repository.GetUserOrganizationForUsers(userIds, cancellationToken);
    }



    #endregion

  }
}