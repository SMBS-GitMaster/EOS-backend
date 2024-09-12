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

    Task<IQueryable<MetricCustomGoalQueryModel>> GetCustomGoalsForMetricsAsync(List<long> recurrencId, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    #endregion

  }

  public partial class DataContext : IDataContext
  {

    #region Queries

    public Task<IQueryable<MetricCustomGoalQueryModel>> GetCustomGoalsForMetricsAsync(List<long> measurableIds, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetCustomGoalsForMetrics(measurableIds, cancellationToken));
    }

    #endregion

    #region Mutations

    #endregion

  }

}
