using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RadialReview.GraphQL.Models
{
  public class MetricScoreQueryModel : RadialReview.Core.GraphQL.Types.ILogProperties
  {

    #region Base Properties

    public long Id { get; set; }

    public int Version { get; set; }

    public string LastUpdatedBy { get; set; }

    public double? DateCreated { get; set; }

    public double DateLastModified { get; set; }

    #endregion

    #region Properties

    public long MeasurableId { get; set; }

    public string Value { get; set; }

    public double? Timestamp { get; set; }

    public string NotesText { get; set; }

    #endregion

    public void Log(ITagCollector collector, string prefix)
    {
      collector.Add($"{prefix}.{nameof(this.Id)}", this.Id);
      collector.Add($"{prefix}.{nameof(this.Version)}", this.Version);
      collector.Add($"{prefix}.{nameof(this.LastUpdatedBy)}", this.LastUpdatedBy);
      collector.Add($"{prefix}.{nameof(this.DateCreated)}", this.DateCreated);
      collector.Add($"{prefix}.{nameof(this.DateLastModified)}", this.DateLastModified);
      collector.Add($"{prefix}.{nameof(this.MeasurableId)}", this.MeasurableId);
      collector.Add($"{prefix}.{nameof(this.Value)}", this.Value);
      collector.Add($"{prefix}.{nameof(this.Timestamp)}", this.Timestamp);
      collector.Add($"{prefix}.{nameof(this.NotesText)}", this.NotesText);
    }
  }
}
