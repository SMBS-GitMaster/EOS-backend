using RadialReview.Core.GraphQL.Enumerations;

namespace RadialReview.GraphQL.Models
{
  public class WorkspaceTileInfo
  {

    public long? MeetingId { get; set; }

    public gqlTileType TileType { get; set; }

  }
}
