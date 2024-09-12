using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Models.Dashboard;
using RadialReview.Utilities;
using System.Collections.Generic;
using System.Text.Json;
using static RadialReview.Accessors.DashboardAccessor;

namespace RadialReview.Repositories
{
  public static class WorkspaceTransformers
  {

    #region Public Methods

    public static WorkspaceStatsTileQueryModel GetDefaultFilterSettings()
    {
      return new WorkspaceStatsTileQueryModel()
      {
        SelectedStatsTileNodes =
              new List<string>() { "TODOS", "ISSUES", "GOALS", "MILESTONES" },
        SelectedStatsTileDateRange = "MONTH"
      };
    }

    public static WorkspaceTilePositionQueryModel GetDefaultPositionSettings()
    {
      return new WorkspaceTilePositionQueryModel(0, 0, 4, 8);
    }

    public static string PositionJSON(int x, int y, int w, int h)
    {
      return JsonSerializer.Serialize(new WorkspaceTilePositionQueryModel(x, y, w, h));
    }

    public static string PositionJSON(WorkspaceTilePositionData source)
    {
      return JsonSerializer.Serialize(new WorkspaceTilePositionQueryModel(source.X, source.Y, source.W, source.H));
    }


    public static WorkspaceQueryModel Transform(this Dashboard source, UserOrganizationModel caller, List<WorkspaceTileQueryModel> tiles, FavoriteModel favoriteData)
    {
      long? favoriteId = favoriteData is null ? null : favoriteData.Id;
      bool favorited = favoriteData is null ? false : true;
      double? favoritedTimestamp = favoriteData is null ? null : favoriteData.CreatedDateTime.ToUnixTimeStamp();
      int? favoritedSortingPosition = favoriteData is null ? null : favoriteData.Position;

      return new WorkspaceQueryModel()
      {
        Id = source.Id,
        Name = source.Title,
        UserId = caller.Id,
        Archived = source.DeleteTime is null ? false : true,
        Tiles = tiles,
        FavoriteId = favoriteId,
        Favorited = favorited,
        FavoritedSortingPosition = favoritedSortingPosition,
        FavoritedTimestamp = favoritedTimestamp,
        CreatedTimestamp = source.CreateTime.ToUnixTimeStamp(),
        LastViewedTimestamp = null,
      };
    }


    public static WorkspaceQueryModel Transform(this DashboardAndTiles source, UserOrganizationModel caller, List<WorkspaceTileQueryModel> tiles, FavoriteModel favoriteData)
    {
      long? favoriteId = favoriteData is null ? null : favoriteData.Id;
      bool favorited = favoriteData is null ? false : true;
      double? favoritedTimestamp = favoriteData is null ? null : favoriteData.CreatedDateTime.ToUnixTimeStamp();
      int? favoritedSortingPosition = favoriteData is null ? null : favoriteData.Position;

      return new WorkspaceQueryModel()
      {
        Id = source.Dashboard.Id,
        Name = source.Dashboard.Title,
        UserId = caller.Id,
        Archived = source.Dashboard.DeleteTime is null ? false : true,
        Tiles = tiles,
        FavoriteId = favoriteId,
        Favorited = favorited,
        FavoritedSortingPosition = favoritedSortingPosition,
        FavoritedTimestamp = favoritedTimestamp,
        CreatedTimestamp = source.Dashboard.CreateTime.ToUnixTimeStamp(),
        LastViewedTimestamp = null,
      };
    }

    public static WorkspaceTileQueryModel Transform(this MeetingTileModel source, long? recurrenceId, string dataUrl)
    {
      return new WorkspaceTileQueryModel()
      {
        Id = source.Id,
        MeetingId = recurrenceId,
        TileType = source.Type.Transform(dataUrl).ToString(),
        WorkspaceId = -1,
        tileSettings = string.IsNullOrEmpty(source.V3StatsFiltering) ? GetDefaultFilterSettings() : JsonSerializer.Deserialize<WorkspaceStatsTileQueryModel>(source.V3StatsFiltering),
        positions = string.IsNullOrEmpty(source.V3Positioning) ? WorkspaceTransformers.GetDefaultPositionSettings() : JsonSerializer.Deserialize<WorkspaceTilePositionQueryModel>(source.V3Positioning),
      };
    }

    public static WorkspaceTileQueryModel Transform(this TileModel source, UserOrganizationModel caller)
    {
      WorkspaceTileInfo tileInfo = null;
      long? recurrenceId = null;
      long parseId;
      if (long.TryParse(source.KeyId, out parseId))
      {
        recurrenceId = parseId;
      }

      if (source.V3Info != null)
      {
        tileInfo = JsonSerializer.Deserialize<WorkspaceTileInfo>(source.V3Info);
      }
      else
      {
        //!! HERE WE GO
        tileInfo = new WorkspaceTileInfo();
        tileInfo.TileType = source.Type.Transform(source.DataUrl);
        switch (tileInfo.TileType)
        {
          case gqlTileType.MEETING_STATS:
          case gqlTileType.MEETING_SOLVED_ISSUES:
          case gqlTileType.MEETING_ISSUES:
          case gqlTileType.MEETING_HEADLINES:
          case gqlTileType.MEETING_GOALS:
          case gqlTileType.MEETING_METRICS:
          case gqlTileType.MEETING_TODOS:
            if (source.KeyId != null)
            {
              tileInfo.MeetingId = long.Parse(source.KeyId);
            }
            break;
          case gqlTileType.MEETING_NOTES:
            var noteId = source.DataUrl.Split('/')[3];
            var note = L10Accessor.GetNote(caller, long.Parse(noteId));
            if (note != null && note.Recurrence != null)
            {
              tileInfo.MeetingId = note.Recurrence.Id;
            }
            break;
        }

        // Apply updates
        source.V3Info = JsonSerializer.Serialize(tileInfo);
        using (var s = HibernateSession.GetCurrentSession())
        {
          using (var tx = s.BeginTransaction())
          {
            s.Update(source);
            tx.Commit();
            s.Flush();
          }
        }
      }

      return new WorkspaceTileQueryModel
      {
        Id = source.Id,
        TileType = tileInfo.TileType.ToString(),
        tileSettings = string.IsNullOrEmpty(source.V3StatsFiltering) ? WorkspaceTransformers.GetDefaultFilterSettings() : JsonSerializer.Deserialize<WorkspaceStatsTileQueryModel>(source.V3StatsFiltering),
        positions = string.IsNullOrEmpty(source.V3Positioning) ? WorkspaceTransformers.GetDefaultPositionSettings() : JsonSerializer.Deserialize<WorkspaceTilePositionQueryModel>(source.V3Positioning),
        MeetingId = tileInfo.MeetingId,
        WorkspaceId = source.Dashboard != null ? source.Dashboard.Id : -1,
      };
    }

    public static string TileTypeUrlHelper(gqlTileType tileType, string meetingId, string noteId)
    {
      // this is for URL tile types
      if (tileType == gqlTileType.PERSONAL_TODOS) return "/TileData/UserTodo2";
      if (tileType == gqlTileType.PERSONAL_METRICS) return "/TileData/UserScorecard2";
      if (tileType == gqlTileType.PERSONAL_GOALS) return "/TileData/UserRock2";
      if (tileType == gqlTileType.MILESTONES) return "/TileData/Milestones";
      if (tileType == gqlTileType.ROLES) return "/TileData/UserRoles";
      if (tileType == gqlTileType.MANAGE) return "/TileData/UserManage2";
      if (tileType == gqlTileType.VALUES) return "/TileData/OrganizationValues";
      if (tileType == gqlTileType.PERSONAL_NOTES) return "/TileData/UserNotes";
      if (tileType == gqlTileType.USER_PROFILE) return "/TileData/UserProfile2";
      if (tileType == gqlTileType.MEETING_METRICS) return "/TileData/L10Scorecard/" + meetingId;
      if (tileType == gqlTileType.MEETING_HEADLINES) return "/TileData/L10Headlines/" + meetingId;
      if (tileType == gqlTileType.MEETING_GOALS) return "/TileData/L10Rocks/" + meetingId;
      if (tileType == gqlTileType.MEETING_TODOS) return "/TileData/L10Todos/" + meetingId;
      if (tileType == gqlTileType.MEETING_ISSUES) return "/TileData/L10Issues/" + meetingId;
      if (tileType == gqlTileType.MEETING_SOLVED_ISSUES) return "/TileData/L10SolvedIssues/" + meetingId;
      if (tileType == gqlTileType.MEETING_STATS) return "/TileData/L10Stats/" + meetingId;
      if (tileType == gqlTileType.MEETING_NOTES) return "/TileData/L10Notes/" + noteId;
      if (tileType == gqlTileType.PROCESSES) return "/TileData/UserProcess";


      return "";
    }

    #endregion

    #region Private Methods

    public static gqlTileType Transform(this TileType tileType, string dataUrl)
    {
      // Modify type
      if (tileType == TileType.Url && !string.IsNullOrEmpty(dataUrl))
      {
        if (dataUrl.StartsWith("/TileData/UserTodo2")) return gqlTileType.PERSONAL_TODOS;
        if (dataUrl.StartsWith("/TileData/UserScorecard2")) return gqlTileType.PERSONAL_METRICS;
        if (dataUrl.StartsWith("/TileData/UserRock2")) return gqlTileType.PERSONAL_GOALS;
        if (dataUrl.StartsWith("/TileData/Milestones")) return gqlTileType.MILESTONES;
        if (dataUrl.StartsWith("/TileData/UserRoles")) return gqlTileType.ROLES;
        if (dataUrl.StartsWith("/TileData/UserManage2")) return gqlTileType.MANAGE;
        if (dataUrl.StartsWith("/TileData/OrganizationValues")) return gqlTileType.VALUES;
        if (dataUrl.StartsWith("/TileData/UserNote")) return gqlTileType.PERSONAL_NOTES;
        if (dataUrl.StartsWith("/TileData/UserProfile2")) return gqlTileType.USER_PROFILE;
        if (dataUrl.StartsWith("/TileData/L10Scorecard/")) return gqlTileType.MEETING_METRICS;
        if (dataUrl.StartsWith("/TileData/L10Headlines/")) return gqlTileType.MEETING_HEADLINES;
        if (dataUrl.StartsWith("/TileData/L10Rocks/")) return gqlTileType.MEETING_GOALS;
        if (dataUrl.StartsWith("/TileData/L10Todos/")) return gqlTileType.MEETING_TODOS;
        if (dataUrl.StartsWith("/TileData/L10Issues/")) return gqlTileType.MEETING_ISSUES;
        if (dataUrl.StartsWith("/TileData/L10SolvedIssues/")) return gqlTileType.MEETING_SOLVED_ISSUES;
        if (dataUrl.StartsWith("/TileData/L10Stats/")) return gqlTileType.MEETING_STATS;
        if (dataUrl.StartsWith("/TileData/L10Notes/")) return gqlTileType.MEETING_NOTES;
        if (dataUrl.StartsWith("/TileData/UserProcess")) return gqlTileType.PROCESSES;
      }

      return (gqlTileType)tileType;
    }

    #endregion

  }
}
