namespace RadialReview.GraphQL.Models
{
  public class WorkspaceTileQueryModel
  {
    public long Id { get; set; }

    public long? MeetingId { get; set; }

    public long WorkspaceId { get; set; }

    public string TileType { get; set; }

    public WorkspaceStatsTileQueryModel tileSettings { get; set; }

    public WorkspaceTilePositionQueryModel positions { get; set; }

  }
}
