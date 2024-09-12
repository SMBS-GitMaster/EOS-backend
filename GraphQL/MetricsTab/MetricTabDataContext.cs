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

    Task<IQueryable<MetricsTabQueryModel>> GetMetricTabsForMeeting(long id, CancellationToken cancellationToken);

    Task<MetricsTabQueryModel> GetMetricTabById(long id, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    #endregion

  }

  public partial class DataContext : IDataContext
  {

    #region Queries

    public async Task<IQueryable<MetricsTabQueryModel>> GetMetricTabsForMeeting(long id, CancellationToken cancellationToken)
    {
      return repository.GetMetricTabsForMeeting(id, cancellationToken);
    }

    public async Task<MetricsTabQueryModel> GetMetricTabById(long id, CancellationToken cancellationToken)
    {
      return repository.GetMetricTabById(id, cancellationToken);
    }

    #endregion

    #region Mutations

    #endregion

  }

}
