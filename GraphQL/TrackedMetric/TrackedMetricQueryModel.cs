using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models
{
  public class TrackedMetricQueryModel
  {

    #region Properties

    public long Id { get; set; }

    public int Version { get; set; }

    public string LastUpdatedBy { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime DateLastModified { get; set; }

    public DateTime? DeleteTime { get; set; }

    public long UserId { get; set; }

    public TrackedMetricColor Color { get; set; }

    public long MetricId { get; set; }

    public long MetricTabId { get; set; }

    public MetricQueryModel Metric { get; set; }

    #endregion

    public static class Associations 
    {
      public enum Metric2 
      {
        Metric
      }
    }
  }
}
