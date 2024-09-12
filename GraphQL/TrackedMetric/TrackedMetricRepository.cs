using RadialReview.Accessors;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries

    IQueryable<TrackedMetricQueryModel> GetTrackedMetricsForTab(long id, CancellationToken cancellationToken);
    IQueryable<TrackedMetricQueryModel> GetTrackedMetricsForTab(List<long> ids, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    #endregion

  }


  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public IQueryable<TrackedMetricQueryModel> GetTrackedMetricsForTab(long id, CancellationToken cancellationToken)
    {
      return MetricTabAccessor.GetTrackedMetricsForTab(caller, id, caller.Id)
        .Select(x => RepositoryTransformers.TransformTrackedMetric(x)).AsQueryable();
    }

    public IQueryable<TrackedMetricQueryModel> GetTrackedMetricsForTab(List<long> ids, CancellationToken cancellationToken)
    {
      return MetricTabAccessor.GetTrackedMetricsForTab(caller, ids, caller.Id)
        .Select(x => RepositoryTransformers.TransformTrackedMetric(x)).AsQueryable();
    }

    #endregion

    #region Mutations

    #endregion

  }
}