using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.GraphQL.Models;

namespace RadialReview.Core.GraphQL.Models.Mutations
{
  public class WorkspaceCreateModel
  {
    public string Name { get; set; }

    public bool? Favorited { get; set; }

    [DefaultValue(null)] public double? FavoritedTimestamp { get; set; }

    [DefaultValue(null)] public int? FavoritedSortingPosition { get; set; }

    public List<WorkspaceTileNodeCreateModel> Tiles { get; set; }

  }

  public class WorkspaceEditModel
  {
    public long WorkspaceId { get; set; }
    [DefaultValue(null)] public string Name { get; set; }
    [DefaultValue(null)] public bool? Favorited { get; set; }
    [DefaultValue(null)] public double? FavoritedTimestamp { get; set; }
    [DefaultValue(null)] public int? FavoritedSortingPosition { get; set; }
    [DefaultValue(null)] public double? LastViewedTimestamp { get; set; }

    public List<WorkspaceTileNodeCreateModel> Tiles { get; set; }

    [DefaultValue(null)] public string PlacementMode { get; set; }
  }

  public class WorkspaceTileNodeCreateModel
  {
    public long? Id { get; set; }

    public long? DashboardId { get; set; }

    public long? MeetingId { get; set; }

    public string Type { get; set; }

    public bool? Archived { get; set; }

    public string Title { get; set; }

    public WorkspaceStatsTileQueryModel TileSettings { get; set; }

    public WorkspaceTilePositionQueryModel Positions { get; set; }

  }

  public class WorkspaceTileNodeEditModel
  {
    public long Id { get; set; }
    public long? MeetingId { get; set; }
    public string Type { get; set; }
    public bool? Archived { get; set; }
    public string Title { get; set; }
    public WorkspaceStatsTileQueryModel TileSettings { get; set; }


  }

  public class WorkspaceTilePositionData
  {
    public long Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
  }

  public class WorkspaceEditTilePositionsModel
  {

    public long WorkspaceId { get; set; }

    public List<WorkspaceTilePositionData> Tiles { get; set; }

  }

}