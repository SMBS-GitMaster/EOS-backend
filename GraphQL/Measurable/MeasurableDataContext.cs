using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RadialReview.Core.GraphQL;
using RadialReview.GraphQL.Models;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    #region Queries
    Task<IQueryable<MeasurableQueryModel>> GetMeasurablesForUserAsync(long userId, CancellationToken cancellationToken);

    Task<IQueryable<MeasurableQueryModel>> GetMeasurablesForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken);

    #endregion

  }

  public partial class DataContext : IDataContext
  {

    #region Queries

    public async Task<IQueryable<MeasurableQueryModel>> GetMeasurablesForUserAsync(long userId, CancellationToken cancellationToken)
    {
      return repository.GetMeasurablesForUser(userId, cancellationToken);
    }

    public async Task<IQueryable<MeasurableQueryModel>> GetMeasurablesForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      return repository.GetMeasurablesForUsers(userIds, cancellationToken);
    }

    #endregion

  }

}
