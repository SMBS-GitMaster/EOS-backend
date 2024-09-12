using Microsoft.Extensions.Logging;
using RadialReview.Core.GraphQL.Types;

namespace RadialReview.GraphQL.Models;

public class OrgChartSeatQueryModel : ILogProperties
{
  public virtual long Id { get; set; }

  public void Log(ITagCollector collector, string prefix)
  {
    collector.Add($"{prefix}.{nameof(this.Id)}", this.Id);
  }

  public static class Collections
  {
    public enum User18
    {
      Users
    }

    public enum OrgChartSeat
    {
      DirectReports
    }

  }

  public static class Associations
  {
    public enum OrgChartPosition
    {
      OrgChartPosition
    }
  }
}