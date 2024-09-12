using RadialReview.GraphQL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.MetricAddExistingLookup
{
  public class MetricAddExistingLookupQueryModel
  {
    #region Base Properties
    public long Id { get; set; }
    public int Version { get; set; }

    public string LastUpdatedBy { get; set; }

    public double? DateCreated { get; set; }

    public double DateLastModified { get; set; }

    public double? LastUpdatedClientTimestamp { get; set; }

    // NOTE: This property is never assigned to.  It is here to force the correct filter type in GraphQL
    public List<MeetingQueryModel> Meetings { get; init; } = null;
    #endregion

    #region Properties
    public string Title { get; set; }
    public UserQueryModel Assignee { get; set; }
    #endregion

    public static class Collections
    {
      public enum Meeting9
      {
        Meetings
      }
    }
      public static class Associations
    {
      public enum User17
      {
        Assignee
      }
    }
  }
}
