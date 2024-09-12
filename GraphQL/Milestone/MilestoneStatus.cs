using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Models.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Models.Query
{
  public enum MilestoneStatus
  {
    INCOMPLETED,
    COMPLETED,
    OVERDUE
  }
  public static class MilestoneStatusExtensions
  {
    public static gqlMilestoneStatus? FromString(this string status)
    {
      if (string.IsNullOrEmpty(status)) return null;
      return Enum.Parse<gqlMilestoneStatus>(status);
    }

    public static gqlMilestoneStatus? ToMilestoneStatus(this BloomMilestoneStatus? status) => status switch
    {
      BloomMilestoneStatus.Incompleted => gqlMilestoneStatus.INCOMPLETED,
      BloomMilestoneStatus.Completed => gqlMilestoneStatus.COMPLETED,
      BloomMilestoneStatus.Overdue => gqlMilestoneStatus.OVERDUE,
      _ => null
    };

    public static BloomMilestoneStatus? ToBloomMilestoneStatus(this gqlMilestoneStatus? status) => status switch
    {
      gqlMilestoneStatus.INCOMPLETED => BloomMilestoneStatus.Incompleted,
      gqlMilestoneStatus.COMPLETED => BloomMilestoneStatus.Completed,
      gqlMilestoneStatus.OVERDUE => BloomMilestoneStatus.Overdue,
      _ => null,
    };

  }
}
