using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    Task<IQueryable<TrackedMetricQueryModel>> GetTrackedMetricsForTab(long id, CancellationToken cancellationToken);
    Task<IQueryable<TrackedMetricQueryModel>> GetTrackedMetricsForTab(List<long> ids, CancellationToken cancellationToken);


  }

  public partial class DataContext : IDataContext
  {

    public async Task<IQueryable<TrackedMetricQueryModel>> GetTrackedMetricsForTab(long id, CancellationToken cancellationToken)
    {
      return repository.GetTrackedMetricsForTab(id, cancellationToken);
    }

    public async Task<IQueryable<TrackedMetricQueryModel>> GetTrackedMetricsForTab(List<long> ids, CancellationToken cancellationToken)
    {
      return repository.GetTrackedMetricsForTab(ids, cancellationToken);
    }

  }

}
