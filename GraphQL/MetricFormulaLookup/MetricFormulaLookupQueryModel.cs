using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.GraphQL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.MetricFormulaLookup
{
  public class MetricFormulaLookupQueryModel
  {
    #region Base Properties
    public long Id { get; set; }
    public int Version { get; set; }

    public string LastUpdatedBy { get; set; }

    public double? DateCreated { get; set; }

    public double DateLastModified { get; set; }

    public double? LastUpdatedClientTimestamp { get; set; }
    #endregion

    #region Properties
    public string Title { get; set; }
    public bool Archived { get; set; }
    public gqlMetricFrequency Frequency { get; set; }
    public UserQueryModel Assignee { get; set; }
    #endregion

    public static class Associations
    {
      public enum User16
      {
        Assignee
      }
    }
  }
}
