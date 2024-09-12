namespace RadialReview.Repositories
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using RadialReview.Core.GraphQL;
  using RadialReview.GraphQL.Models;

  public partial interface IDataContext
  {

    Task<bool> GetMetricTabPinned(long metricTabId, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public async Task<bool> GetMetricTabPinned(long metricTabId, CancellationToken cancellationToken)
    {
      return repository.GetMetricTabPinned(metricTabId, cancellationToken);
    }

  }

}
