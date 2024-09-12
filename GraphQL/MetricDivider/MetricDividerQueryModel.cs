using RadialReview.Core.GraphQL.Enumerations;

namespace RadialReview.GraphQL.Models
{
  public class MetricDividerQueryModel
  {

    #region Base Properties
    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }

    #endregion

    #region Properties

    public gqlMetricFrequency Frequency { get; set; }

    public int IndexInTable { get; set; }

    public string Title { get; set; }
    public int Height { get; set; }

    #endregion

    public static class Associations
    {
      public enum User9
      {
        User
      }
    }

  }
}
