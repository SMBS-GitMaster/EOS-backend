namespace RadialReview.Repositories
{
  using RadialReview.Accessors;
  using RadialReview.Models;
  using System.Threading;

  public partial interface IRadialReviewRepository
  {

    bool GetMetricTabPinned(long metricTabId, CancellationToken cancellationToken);

  }

  public partial class RadialReviewRepository
  {

    public bool GetMetricTabPinned(long metricTabId, CancellationToken cancellationToken)
    {
      MetricsTabPinnedModel result = MetricsTabPinnedAccessor.GetPinnedForMetric(caller, metricTabId);
      if (result == null)
      {
        return false;
      }
      return result.IsPinnedToTabBar;
    }

  }


}
