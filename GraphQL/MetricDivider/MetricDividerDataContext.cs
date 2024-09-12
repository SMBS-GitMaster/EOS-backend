using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    Task<IQueryable<MetricDividerQueryModel>> GetMetricDividersForMeetingAsync(long recurrencId, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public Task<IQueryable<MetricDividerQueryModel>> GetMetricDividersForMeetingAsync(long recurrencId, CancellationToken cancellationToken)
    {
      List<MetricDividerQueryModel> result = new List<MetricDividerQueryModel>();
      return Task.FromResult(result.AsQueryable());
    }

  }

}
