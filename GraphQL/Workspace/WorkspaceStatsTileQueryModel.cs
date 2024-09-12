using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RadialReview.GraphQL.Models
{
  public class WorkspaceStatsTileQueryModel
  {

    [JsonPropertyName("SelectedNodes")]
    public List<string> SelectedStatsTileNodes { get; set; }

    [JsonPropertyName("SelectedDateRange")]
    public string SelectedStatsTileDateRange { get; set; }

    public string SelectedNoteId { get; set; }

  }
}
