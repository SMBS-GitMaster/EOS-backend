namespace RadialReview.GraphQL.Models;

using System;
using Microsoft.Extensions.Logging;
using RadialReview.Core.GraphQL.Types;

public class OrgChartQueryModel : ILogProperties
{
  public long Id {get; set;}
  public string Name {get; set;}
  public DateTime CreateTime {get; set;}
  public DateTime? DeleteTime {get; set;}

  public void Log(ITagCollector collector, string prefix)
  {
    collector.Add($"{prefix}.{nameof(this.Id)}", this.Id);
    collector.Add($"{prefix}.{nameof(this.Name)}", this.Name);
    collector.Add($"{prefix}.{nameof(this.CreateTime)}", this.CreateTime);
    collector.Add($"{prefix}.{nameof(this.DeleteTime)}", this.DeleteTime);
  }

  public static class Collections
  {
    public enum OrgChartSeat2
    {
      Seats
    }
  }
}