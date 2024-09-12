using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RadialReview.Core.Models.Scorecard;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
  internal class MetricCustomGoalAccessor
  {
    internal static List<MetricCustomGoal> GetCustomGoalsForMetric(long measurableId, CancellationToken cancellationToken)
    {
      using (var session = HibernateSession.GetCurrentSession())
      {
        var results = session.QueryOver<MetricCustomGoal>().Where(x => x.DeleteTime == null && x.MeasurableId == measurableId).List().ToList();
        return results;
      }
    }

    internal static List<MetricCustomGoal> GetCustomGoalsForMetrics(List<long> measurableIds, CancellationToken cancellationToken)
    {
      using (var session = HibernateSession.GetCurrentSession())
      {
        var results = session.QueryOver<MetricCustomGoal>().Where(x => x.DeleteTime == null)
           .WhereRestrictionOn(x => x.MeasurableId).IsIn(measurableIds)
          .List().ToList();
        return results;
      }
    }
  }
}
