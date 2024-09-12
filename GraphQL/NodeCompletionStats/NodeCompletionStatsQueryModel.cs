using System;
using System.Collections.Generic;

namespace RadialReview.GraphQL.Models
{
  public class NodeCompletionStatsQueryModel
  {

    #region Base Properties

    public long? RecurrenceId { get; set; }

    public long UserId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string GroupBy { get; set; }

    #endregion

    #region Properties

    //public List<NodeStatDataQueryModel> Todos { get; set; }

    //public List<NodeStatDataQueryModel> Issues { get; set; }

    //public List<NodeStatDataQueryModel> Goals { get; set; }

    //public List<NodeStatDataQueryModel> Milestones { get; set; }

    #endregion

  }
}
