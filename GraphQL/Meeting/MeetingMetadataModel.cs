using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadialReview.Core.GraphQL.Types;

namespace RadialReview.GraphQL.Models {
  public class MeetingMetadataModel : ILogProperties {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }

    #endregion
    public string Name { get; set; }
    public double? FavoritedTimestamp { get; set; }
    public int FavoritedSortingPosition { get; set; }

    public string MeetingType { get; set; }


    #region Should these be here??

    public long UserId { get; set; }
    #endregion

    public void Log(ITagCollector collector, string prefix)
    {
      collector.Add($"{prefix}.{nameof(this.Id)}", this.Id);
    }
  }
}
