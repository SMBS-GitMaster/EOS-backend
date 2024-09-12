using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {
    Task<IQueryable<NotificationQueryModel>> GetNotificationsForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public async Task<IQueryable<NotificationQueryModel>> GetNotificationsForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      return repository.GetNotificationsForUsers(userIds, cancellationToken);
    }

  }
}
