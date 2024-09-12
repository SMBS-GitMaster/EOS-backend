using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    Task<IQueryable<MetricScoreQueryModel>> GetScoresForMeasurablesAsync(IEnumerable<long> measurableId, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public async Task<IQueryable<MetricScoreQueryModel>> GetScoresForMeasurablesAsync(IEnumerable<long> measurableId, CancellationToken cancellationToken)
    {
      return repository.GetScoresForMeasurables(measurableId, cancellationToken);
    }

  }
}
