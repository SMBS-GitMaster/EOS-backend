namespace RadialReview.Core.GraphQL;

using RadialReview.GraphQL.Models;
using RadialReview.Models.Accountability;


public static class OrgChartTransformer
{
  public static OrgChartQueryModel Transform(this AccountabilityChart orgChart)
  {
    var result =
        new OrgChartQueryModel
        {
          Id = orgChart.Id,
        };

    return result;
  }
}