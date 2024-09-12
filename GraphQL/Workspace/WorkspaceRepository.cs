using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.Repositories;
using RadialReview.Crosscutting.Hooks;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Models;
using RadialReview.Models.Dashboard;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries
    WorkspaceQueryModel GetWorkspace(long id, CancellationToken cancellationToken);

    IQueryable<WorkspaceQueryModel> GetWorkspaces(long userId, CancellationToken cancellationToken);

    IQueryable<WorkspaceQueryModel> GetWorkspacesForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken);

    WorkspaceQueryModel GetWorkspaceForMeeting(long recurrenceId, CancellationToken cancellationToken);

    WorkspaceTileQueryModel GetMeetingWorkspaceTile(long id, CancellationToken cancellationToken);

    WorkspaceTileQueryModel GetPersonalWorkspaceTile(long id, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<IdModel> CreateWorkspace(WorkspaceCreateModel workspaceCreateModel);

    Task<IdModel> EditWorkspace(WorkspaceEditModel workspaceEditModel);

    Task<IdModel> CreateWorkspaceTile(WorkspaceTileNodeCreateModel workspaceTileCreateModel);

    Task<IdModel> EditWorkspaceTile(WorkspaceTileNodeEditModel workspaceTileEditModel);

    Task<GraphQLResponseBase> EditWorkspaceTilePositions(WorkspaceEditTilePositionsModel input);

    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public WorkspaceQueryModel GetWorkspace(long id, CancellationToken cancellationToken)
    {
      //throw new Exception("Never rely on user supplied userId");
      //throw new Exception("fix issues from obsolete tag");
      var result = GetWorkspaceForUser(id, cancellationToken);
      return result;
    }

    public IQueryable<WorkspaceQueryModel> GetWorkspaces(long userId, CancellationToken cancellationToken)
    {
      //throw new Exception("Never rely on user supplied userId");
      //throw new Exception("fix issues from obsolete tag");
      var results = GetWorkspacesForUser(userId, cancellationToken);
      return results.AsQueryable();
    }

    protected WorkspaceQueryModel GetWorkspaceForUser(long id, CancellationToken cancellationToken)
    {
      return GetWorkspaceWithFavorite(caller, id);
    }

    protected List<WorkspaceQueryModel> GetWorkspacesForUser(long userId, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {

          var perms = PermissionsUtility.Create(s, caller);

          var workspacesData = DashboardAccessor.GetWorkspaceDropdown(s, perms, userId);
          if (workspacesData == null)
            return new List<WorkspaceQueryModel>();

          var results = new List<WorkspaceQueryModel>();
          const string primaryWorkspaceName = "Primary Workspace";

          var pivotFavoriteWorkspaces = caller.Id == userId ?
            FavoriteAccessor.GetFavoritesForUser(caller, userId, Models.FavoriteType.Workspace)
            : Enumerable.Empty<Models.FavoriteModel>().ToList();
          var pivotFavoritePrimaryWorkspace = pivotFavoriteWorkspaces.Find(fw => fw.ParentId == workspacesData.PrimaryWorkspace.WorkspaceId);

          // Primary Workspace
          results.Add(new WorkspaceQueryModel()
          {
            Id = workspacesData.PrimaryWorkspace.WorkspaceId,
            Name = primaryWorkspaceName,
            UserId = userId,
            FavoriteId = pivotFavoritePrimaryWorkspace?.Id,
            FavoritedSortingPosition = pivotFavoritePrimaryWorkspace?.Position,
            FavoritedTimestamp = pivotFavoritePrimaryWorkspace?.CreatedDateTime.ToUnixTimeStamp(),
            IsPrimary = true,
            WorkspaceParentId = caller.PrimaryWorkspace?.WorkspaceId,
            WorkspaceType = caller.PrimaryWorkspace != null ? (gqlDashboardType)caller.PrimaryWorkspace.Type : gqlDashboardType.STANDARD,
          });

          // Custom Workspaces
          if (workspacesData.CustomWorkspaces != null)
          {
            // delete default workspaces from custom workspaces
            var workspaceWithoutDefault = workspacesData.CustomWorkspaces.Where((x) => x.Id != workspacesData.PrimaryWorkspace.WorkspaceId).ToList();

            results.AddRange(workspaceWithoutDefault.Select(x =>
            {
              return GetWorkspaceForUser(x.Id, cancellationToken);
            }));
          }

          return results;
        }
      }
    }

    public IQueryable<WorkspaceQueryModel> GetWorkspacesForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        //throw new Exception("never rely on user supplied userIds. caller.User.Id is a permission checked userId");
        //throw new Exception("fix issues from obsolete tag");
        List<WorkspaceQueryModel> results = new List<WorkspaceQueryModel>();
        foreach (var userId in userIds)
        {
          results.AddRange(GetWorkspacesForUser(userId, cancellationToken));
        }

        return results;
      });
    }

    public WorkspaceQueryModel GetWorkspaceForMeeting(long recurrenceId, CancellationToken cancellationToken)
    {
      var dashboardAndTiles = DashboardAccessor.GenerateDashboard(caller, recurrenceId, Models.Enums.DashboardType.L10, null);
      var favorite = FavoriteAccessor.GetFavoriteForUser(caller, Models.FavoriteType.Workspace, dashboardAndTiles.Dashboard.Id);

      List<WorkspaceTileQueryModel> tilesDTO = new List<WorkspaceTileQueryModel>();
      tilesDTO.Add(GetOrCreateMeetingUserTileSettings(recurrenceId, "Stats", gqlTileType.MEETING_STATS));

      return dashboardAndTiles.Transform(caller, tilesDTO, favorite);
    }

    public WorkspaceTileQueryModel GetPersonalWorkspaceTile(long id, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var tile = s.Get<TileModel>(id);
          return tile.Transform(caller);
        }
      }
    }

    public WorkspaceTileQueryModel GetMeetingWorkspaceTile(long id, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var tile = s.Get<MeetingTileModel>(id);
          return tile.Transform(null, null);
        }
      }
    }

    #endregion

    #region Mutations

    public async Task<IdModel> CreateWorkspace(WorkspaceCreateModel model)
    {
      ErrorOnNonDefault(model, x => x.Favorited);
      ErrorOnNonDefault(model, x => x.FavoritedTimestamp);
      ErrorOnNonDefault(model, x => x.FavoritedSortingPosition);
      var primary = false; //todo
      var prepopulate = false;//todo
      var dash = DashboardAccessor.CreateDashboard(caller, model.Name, primary, prepopulate);

      if (model.Favorited != null && model.Favorited.Value)
      {
        await CreateFavorite(new FavoriteCreateMutationModel
        {
          ParentId = dash.Id,
          ParentType = FavoriteType.Workspace.ToString(),
          Position = model.FavoritedSortingPosition.Value,
          PostedTimestamp = DateTime.UtcNow.ToUnixTimeStamp(),
          User = caller.Id
        });
      }

      var workspace = dash.TransformDashboard();

      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          if (model.Tiles != null)
          {
            string noteId = null;
            var tiles = DashboardAccessor.GetTiles(s, workspace.Id);
            var tileWithlargestY = tiles.MaxBy(x => x.Y);
            int v1LargestY = 0;
            if (tileWithlargestY != null) v1LargestY = tileWithlargestY.Y;

            var positions = WorkspaceTransformers.GetDefaultPositionSettings();
            var v3LargestY = positions.Y;

            foreach (var tile in model.Tiles)
            {
              v1LargestY = v1LargestY + 8;
              v3LargestY = v3LargestY + 8;
              Enum.TryParse(tile.Type, out gqlTileType tileTypeEnum);

              if (tileTypeEnum == gqlTileType.MEETING_NOTES)
              {
                var padId = await L10Accessor.CreateNote(caller, tile.MeetingId.Value, "");
                noteId = L10Accessor.GetVisibleL10Notes_Unsafe(new List<long> { tile.MeetingId.Value }).Where(x => x.PadId == padId).Select(x => x.Id).FirstOrDefault().ToString();
              }
              if (tileTypeEnum == gqlTileType.PERSONAL_NOTES)
              {
                var note = await PersonalNoteAccessor.CreatePersonalNote(caller, workspace.Id, "");
                noteId = note.Id.ToString();
              }

              positions.Y = v3LargestY;
              var V3Positions = JsonSerializer.Serialize(positions);
              var dataUrl = WorkspaceTransformers.TileTypeUrlHelper(tileTypeEnum, tile.MeetingId.ToString(), noteId);
              var tileResult = DashboardAccessor.CreateTile(caller, dash.Id, 2, 5, 0, v1LargestY, dataUrl, "", (Models.Dashboard.TileType)tileTypeEnum, tile.MeetingId.ToString(), V3Positions);
              await HooksRegistry.Each<IWorkspaceTileHook>((ses, x) => x.InsertWorkspaceTile(ses, caller, tileResult.Transform(caller), workspace));
            }
          }
          await HooksRegistry.Each<IWorkspaceHook>((ses, x) => x.CreateWorkspace(ses, caller, dash.TransformDashboard(), caller.Id));
        }
      }

      return new IdModel(dash.Id);
    }

    public async Task<IdModel> EditWorkspace(WorkspaceEditModel model)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          ErrorOnNonDefault(model, x => x.LastViewedTimestamp);
          var dashboard = s.Get<Dashboard>(model.WorkspaceId);

          if (model.Name != null)
          {
            DashboardAccessor.RenameDashboard(caller, model.WorkspaceId, model.Name);
          }

          if (model.Favorited != null)
          {
            var curFavorite = FavoriteAccessor.GetFavoriteForUser(caller, FavoriteType.Workspace, model.WorkspaceId);

            if (model.Favorited.Value)
            {
              if (curFavorite == null)
              {
                await CreateFavorite(new FavoriteCreateMutationModel
                {
                  ParentId = model.WorkspaceId,
                  ParentType = Models.FavoriteType.Workspace.ToString(),
                  Position = model.FavoritedSortingPosition.Value,
                  PostedTimestamp = DateTime.UtcNow.ToUnixTimeStamp(),
                  User = caller.Id
                });
              }
              else
              {
                await EditFavorite(new FavoriteEditMutationModel
                {
                  FavoriteId = curFavorite.Id,
                  ParentId = curFavorite.ParentId,
                  ParentType = curFavorite.ParentType.ToString(),
                  Position = model.FavoritedSortingPosition.Value,
                  User = curFavorite.UserId
                });
              }
            }
            else
            {
              await DeleteFavorite(new FavoriteDeleteMutationModel
              {
                FavoriteId = curFavorite.Id,
                ParentType = curFavorite.ParentType.ToString(),
              });
            }
          }

          if (model.Tiles != null)
          {
            var workspaceTiles = DashboardAccessor.GetTiles(s, model.WorkspaceId);

            List<TileModel> tilesWithinQuery = new List<TileModel>();
            List<TileModel> tilesToArchive = new List<TileModel>();
            List<WorkspaceTilePositionQueryModel> occupiedLocations = new List<WorkspaceTilePositionQueryModel>();
            var possibleTilesToArchive = workspaceTiles.Where(x => x.Hidden == false).ToList();
            var tilesToKeep = model.Tiles.Where(x => x.Id != null).ToList();
            var idsToKeep = tilesToKeep.Select(x => x.Id).ToList();

            // Get a list of tiles we *may* archive
            foreach (var tile in possibleTilesToArchive)
            {
              if (idsToKeep.Contains(tile.Id))
              {
                tilesWithinQuery.Add(tile);
              }
              else
              {
                tilesToArchive.Add(tile);
              }
            }

            // Hide the removed tiles first
            foreach (var tile in tilesToArchive)
            {
              DashboardAccessor.EditTile(caller, tile.Id, tile.Height, tile.Width, tile.X, tile.Y,
                  true, tile.DataUrl, tile.Title);
            }

            // We never care about meeting/personal after this and they call the same mutation (removed)
            // We already know x.Hidden = false because it comes from possibleTilesToArchive which already checks (removed)

            // Update existing tiles & build rect list
            WorkspaceTilePositionQueryModel position;
            foreach (var tile in tilesWithinQuery)
            {
              // Get value from list
              var inputModelTile = model.Tiles.Where(x => x.Id == tile.Id).Single();

              position = inputModelTile.Positions != null ? inputModelTile.Positions : !string.IsNullOrEmpty(tile.V3Positioning) ? JsonSerializer.Deserialize<WorkspaceTilePositionQueryModel>(tile.V3Positioning) : new WorkspaceTilePositionQueryModel(tile.X, tile.Y, tile.Width, tile.Height);

              // model.Tiles -> possibleTilesToArchive -> tilesWithinQuery
              // This is *never* be null
              DashboardAccessor.EditTile(caller, tile.Id, position.H, position.W, position.X, position.Y,
                inputModelTile.Archived, tile.DataUrl, inputModelTile.Title,
                JsonSerializer.Serialize(position));

              // Add to occupied rects
              occupiedLocations.Add(position);
            }

            // NEW TILES TO CREATE
            var newTilesToCreate = model.Tiles.Where(x => x.Id == null);
            foreach (var tile in newTilesToCreate)
            {
              string noteId = null;
              TileType v1TileType = (TileType)EnumHelper.ConvertToNonNullableEnum<gqlTileType>(tile.Type);
              Enum.TryParse(tile.Type, out gqlTileType tileTypeEnum);

              if (tileTypeEnum == gqlTileType.MEETING_NOTES)
              {
                var padId = await L10Accessor.CreateNote(caller, tile.MeetingId.Value, "");
                noteId = L10Accessor.GetVisibleL10Notes_Unsafe(new List<long> { tile.MeetingId.Value }).Where(x => x.PadId == padId).Select(x => x.Id).FirstOrDefault().ToString();
              }
              if (tileTypeEnum == gqlTileType.PERSONAL_NOTES)
              {
                var note = await PersonalNoteAccessor.CreatePersonalNote(caller, model.WorkspaceId, "");
                noteId = note.Id.ToString();
              }

              // Get positioning
              position = WorkspaceTransformers.GetDefaultPositionSettings();

              gqlTilePlacementMode placementMode = gqlTilePlacementMode.FIND_BEST_FIT;
              if (!string.IsNullOrWhiteSpace(model.PlacementMode))
              {
                placementMode = EnumHelper.ConvertToNonNullableEnum<gqlTilePlacementMode>(model.PlacementMode);
              }

              switch (placementMode)
              {
                case gqlTilePlacementMode.FIND_BEST_FIT:
                  FindBestFit(occupiedLocations, ref position, 12);
                  break;
                case gqlTilePlacementMode.SET_FIRST_FREE_Y:
                  SetFirstFreeY(occupiedLocations, ref position);
                  break;
                case gqlTilePlacementMode.SET_NEW_LINE_Y:
                  SetNewLineY(occupiedLocations, ref position);
                  break;
              }

              occupiedLocations.Add(position);

              var dataUrl = WorkspaceTransformers.TileTypeUrlHelper(tileTypeEnum, tile.MeetingId.ToString(), noteId);
              DashboardAccessor.CreateTile(caller, model.WorkspaceId, position.W, position.H, position.X, position.Y, dataUrl, tile.Title, v1TileType, tile.MeetingId.ToString(), JsonSerializer.Serialize(position));

            }
          }

          await HooksRegistry.Each<IWorkspaceHook>((ses, x) => x.UpdateWorkspace(ses, caller, GetWorkspace(model.WorkspaceId, new CancellationTokenSource().Token)));
        }
      }

      return new IdModel(model.WorkspaceId);
    }

    public async Task<IdModel> CreateWorkspaceTile(WorkspaceTileNodeCreateModel model)
    {
      TileType v1TileType = (TileType)EnumHelper.ConvertToNonNullableEnum<gqlTileType>(model.Type);// (TileType)EnumHelper.ConvertToNonNullable<gqlTileType>(model.Type);

      if (model.TileSettings == null)
      {
        model.TileSettings = WorkspaceTransformers.GetDefaultFilterSettings();
      }

      if (model.Positions == null)
      {
        model.Positions = WorkspaceTransformers.GetDefaultPositionSettings();
      }

      string jsonFilteringSettings = JsonSerializer.Serialize(model.TileSettings);
      string jsonPositioning = JsonSerializer.Serialize(model.Positions);

      long dashboardId = model.DashboardId.HasValue ? model.DashboardId.Value : -1;
      if (model.MeetingId.HasValue)
      {
        var tile = DashboardAccessor.CreateMeetingTile(caller, "", v1TileType, model.MeetingId.Value.ToString(),
        jsonPositioning, jsonFilteringSettings);
        return new IdModel(tile.Id);
      }
      else
      {
        throw new NotImplementedException();
      }
    }

    public async Task<IdModel> EditWorkspaceTile(WorkspaceTileNodeEditModel model)
    {
      if (model.MeetingId.HasValue)
      {
        DashboardAccessor.EditMeetingTile(caller, model.Id, null, null, model.MeetingId.Value.ToString(), null, JsonSerializer.Serialize(model.TileSettings));

        var dashboardAndTiles = DashboardAccessor.GenerateDashboard(caller, model.MeetingId.Value, Models.Enums.DashboardType.L10, null);

        using (var s = HibernateSession.GetCurrentSession())
        {
          using (var tx = s.BeginTransaction())
          {
            var meetingTile = s.Get<MeetingTileModel>(model.Id);
            long recurrenceId = -1;
            long.TryParse(meetingTile.KeyId, out recurrenceId);
            WorkspaceTileQueryModel tile = new WorkspaceTileQueryModel()
            {
              Id = meetingTile.Id,
              MeetingId = recurrenceId,
              TileType = model.Type == null ? ((gqlTileType)meetingTile.Type).ToString() : model.Type,
              WorkspaceId = -1,
              tileSettings = string.IsNullOrEmpty(meetingTile.V3StatsFiltering) ? WorkspaceTransformers.GetDefaultFilterSettings() : JsonSerializer.Deserialize<WorkspaceStatsTileQueryModel>(meetingTile.V3StatsFiltering),
              positions = string.IsNullOrEmpty(meetingTile.V3Positioning) ? WorkspaceTransformers.GetDefaultPositionSettings() : JsonSerializer.Deserialize<WorkspaceTilePositionQueryModel>(meetingTile.V3Positioning),
            };

            WorkspaceQueryModel workspace = dashboardAndTiles.Dashboard.TransformDashboard();
            workspace.Tiles = new List<WorkspaceTileQueryModel>()
          {
            tile
          };

            await HooksRegistry.Each<IWorkspaceHook>((ses, x) => x.UpdateMeetingWorkspace(ses, caller, workspace, model.MeetingId.Value));
            await HooksRegistry.Each<IWorkspaceTileHook>((ses, x) => x.UpdateWorkspaceTile(ses, caller, tile));
          }
        }
      }
      else
      {
        DashboardAccessor.EditWorkspacePersonalTile(caller, model);
      }

      return new IdModel(model.Id);
    }

    public async Task<GraphQLResponseBase> EditWorkspaceTilePositions(WorkspaceEditTilePositionsModel model)
    {
      try
      {
        using (var s = HibernateSession.GetCurrentSession())
        {
          using (var tx = s.BeginTransaction())
          {

            var dashboard = DashboardAccessor.GetDashboard(caller, model.WorkspaceId);
            var workspace = WorkspaceTransformers.Transform(dashboard, caller, null, null);
            List<TileModel> updatedTiles = new List<TileModel>();

            // Update tile positions (without sending messages
            foreach (var tile in model.Tiles)
            {
              var wTile = DashboardAccessor.EditTilePositionsV3(caller, tile.Id, WorkspaceTransformers.PositionJSON(tile));
              updatedTiles.Add(wTile);
            }

            // Send messages after all updates (prevent flickering)
            foreach (var wTile in updatedTiles)
            {
              if (workspace.WorkspaceParentId != null && workspace.WorkspaceType == gqlDashboardType.MEETING)
              {
                await HooksRegistry.Each<IWorkspaceHook>((ses, x) => x.UpdateMeetingWorkspace(ses, caller, workspace, workspace.WorkspaceParentId.Value));
              }
              await HooksRegistry.Each<IWorkspaceTileHook>((ses, x) => x.UpdateWorkspaceTile(ses, caller, wTile.Transform(caller)));
            }

            return new GraphQLResponseBase(success: true, "Positions updated");
          }

        }
      }
      catch (Exception ex)
      {
        return GraphQLResponseBase.Error(ex);
      }
    }

    #endregion

    #region Private Methods

    private WorkspaceTileQueryModel GetOrCreateMeetingUserTileSettings(long recurrenceId, string title, gqlTileType tileType)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        var result = s.QueryOver<MeetingTileModel>().Where(_ => _.ForUser.Id == caller.User.Id && _.KeyId == recurrenceId.ToString()).List().FirstOrDefault();
        if (result == null)
        {
          // We need to insert
          var tile = DashboardAccessor.CreateMeetingTile(caller, title, (Models.Dashboard.TileType)tileType, recurrenceId.ToString(), "", JsonSerializer.Serialize(WorkspaceTransformers.GetDefaultFilterSettings()));
          return tile.Transform(recurrenceId, null);
        }
        else
        {
          return result.Transform(recurrenceId, null);
        }
      }
    }

    /// <summary>
    /// Find the best location to place a new tile
    /// </summary>
    /// <param name="occupied"></param>
    /// <param name="result"></param>
    /// <param name="maximumX"></param>
    private void FindBestFit(List<WorkspaceTilePositionQueryModel> occupied, ref WorkspaceTilePositionQueryModel result, int maximumX)
    {
      WorkspaceTilePositionQueryModel validate = new WorkspaceTilePositionQueryModel();
      if (result.W > maximumX) result.W = maximumX;
      validate.W = result.W;
      validate.H = result.H;
      validate.X = result.X = 0;
      validate.Y = result.Y = 0;

      while (true)
      {
        // Check X positioning on this line
        var overlaps = occupied.Where(_ => _.Overlaps(validate)).ToList();
        if (overlaps.Count == 0)
        {
          result.X = validate.X;
          result.Y = validate.Y;
          return;
        }
        else
        {
          validate.X = overlaps.OrderBy(_ => _.Right).First().Right;
          if(validate.Right > maximumX)
          {
            validate.X = 0;
            validate.Y += 1;
          }
        }

        // Remove tiles that end above our current Y evaluation line
        overlaps = overlaps.Where(_ => _.Bottom < validate.Y).ToList();
      }
    }

    /// <summary>
    /// Find the first available empty line to place tile on
    /// </summary>
    /// <param name="occupied"></param>
    /// <param name="result"></param>
    private void SetFirstFreeY(List<WorkspaceTilePositionQueryModel> occupied, ref WorkspaceTilePositionQueryModel result)
    {
      if (occupied.Count == 0)
      {
        result.Y = 0;
        return;
      }

      var sortedOccupied = occupied.OrderBy(r => r.Bottom).ToList();
      for (int i = 0; i < sortedOccupied.Count - 1; i++)
      {
        if (sortedOccupied[i + 1].Y - sortedOccupied[i].Bottom >= result.H)
        {
          result.Y = sortedOccupied[i].Bottom;
          return;
        }
      }

      result.Y = sortedOccupied[sortedOccupied.Count - 1].Bottom;
    }

    /// <summary>
    /// Place tile on a brand new line below everything else
    /// </summary>
    /// <param name="occupied"></param>
    /// <param name="result"></param>
    private void SetNewLineY(List<WorkspaceTilePositionQueryModel> occupied, ref WorkspaceTilePositionQueryModel result)
    {
      if (occupied.Count == 0)
      {
        result.Y = 0;
        return;
      }

      int y = 0;
      int y2;
      foreach (var item in occupied)
      {
        y2 = item.Y + item.H;
        if (y2 > y) y = y2;
      }

      result.Y = y;
    }

    #endregion

    #region Support Methods

    public static WorkspaceQueryModel GetWorkspaceWithFavorite(UserOrganizationModel caller, long id)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {

          var perms = PermissionsUtility.Create(s, caller);

          var workspace = DashboardAccessor.GetTilesAndDashboard(caller, id);
          if (workspace == null)
            return new WorkspaceQueryModel();

          var favoriteData = FavoriteAccessor.GetFavoriteForUser(caller, Models.FavoriteType.Workspace, id);

          var workspaceQueryModelTiles = new List<WorkspaceTileQueryModel>();
          foreach (var tile in workspace.Tiles)
          {
            var v3Tile = tile.Transform(caller);
            if (v3Tile.MeetingId.HasValue)
            {
              try
              {
                perms.CanView(PermItem.ResourceType.L10Recurrence, v3Tile.MeetingId.Value);
                workspaceQueryModelTiles.Add(v3Tile);
              }
              catch
              {
                DashboardAccessor.EditTile(caller, tile.Id, hidden: true);
              }
            }
            else
            {
              workspaceQueryModelTiles.Add(v3Tile);
            }
          }

          var result = workspace.Transform(caller, workspaceQueryModelTiles, favoriteData);
          return result;
        }
      }
    }

    #endregion

  }
}