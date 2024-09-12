namespace RadialReview.GraphQL.Models
{
  public class MetricDataModel
  {

    #region Properties
    public MetricCumulativeDataModel CumulativeData { get; set; }

    public MetricAverageDataModel AverageData { get; set; }

    public MetricProgressiveDataModel ProgressiveData { get; set; }

    #endregion

  }
}
