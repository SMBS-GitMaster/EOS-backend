using RadialReview.Core.GraphQL.Enumerations;

namespace RadialReview.GraphQL.Models
{
  public class MetricCustomGoalQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLateModified { get; set; }
    public double LateUpdatedClientTimestemp { get; set; }
    public string Type { get { return "metricCustomGoal"; } }

    #endregion

    #region Properties

    public string Title { get; set; }

    public gqlMetricFrequency Frequency { get; set; }

    public RadialReview.Models.Enums.UnitType Units { get; set; }

    public RadialReview.Models.Enums.LessGreater Rule { get; set; }

    public string SingleGoalValue { get; set; }

    public string MinGoalValue { get; set; }

    public string MaxGoalValue { get; set; }

    public double? StartDate { get; set; }

    public double? EndDate { get; set; }

    public double? DateDeleted { get; set; }

    public long MeasurableId { get; set; }

    #endregion

  }
}
