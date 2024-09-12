using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models
{
  public class MetricsTabQueryModel
  {

    #region Properties

    public long Id { get; set; }

    public int Version { get; set; }

    public string LastUpdatedBy { get; set; }

    public DateTime DateCreated { get; set; }

    public DateTime DateLastModified { get; set; }

    public DateTime? DeleteTime { get; set; }

    public long UserId { get; set; }

    public UserQueryModel Creator { get; set; }

    public string Name { get; set; }

    public UnitType Units { get; set; }

    public Frequency Frequency { get; set; }

    public bool IsSharedToMeeting { get; set; }

    public long? MeetingId { get; set; }

    public bool IsPinnedToTabBar { get; set; }

    #endregion


    public static class Collections
    {
      public enum TrackedMetric
      {
        TrackedMetrics
      }
    }

    public static class Associations 
    {

      public enum User15 
      {
        Creator
      }

      public enum Metric 
      {
        Metric
      }

      public enum Meeting6
      {
        Meeting
      }
    }
  }
}
