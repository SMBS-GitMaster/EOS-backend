using Microsoft.CodeAnalysis.CSharp.Syntax;
using NHibernate;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.Repositories;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.ViewModels;
using RadialReview.Repositories;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TileVM = RadialReview.Core.Controllers.DashboardController.TileVM;

namespace RadialReview.Accessors {
	public class DashboardAccessor {

		public static int TILE_HEIGHT = 5;

		public static List<Dashboard> GetDashboardsForUser(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetDashboardsForUser(s, perms, userId);

				}
			}
		}

		public static WorkspaceDropdownVM GetWorkspaceDropdown(ISession s, PermissionsUtility perms, long userId) {
			perms.Self(userId);
			var allDashboards = DashboardAccessor.GetDashboardsForUser(s, perms, userId);
			var l10s = L10Accessor.GetViewableL10Meetings_Tiny(s, perms, userId).OrderByDescending(x => x.StarDate).ThenBy(x => x.Name).ToList();
			var user = s.Get<UserOrganizationModel>(userId);
			var primaryDash = user.PrimaryWorkspace ?? new UserOrganizationModel.PrimaryWorkspaceModel() {
				WorkspaceId = allDashboards.Where(x => x.PrimaryDashboard).Select(x => x.Id).FirstOrDefault(),
				Type = DashboardType.Standard
			};
			var custom = allDashboards.Where(x => !x.PrimaryDashboard).Select(x => new NameId(x.Title, x.Id)).ToList();

			var defaultWorkspaceName = "Default Workspace";
			if (user.UserIds.Length > 1)
				defaultWorkspaceName = "Default Workspace (cross-account)";

			var originals = s.QueryOver<Dashboard>()
				.Where(x => x.DeleteTime == null && x.ForUser.Id == user.User.Id && x.PrimaryDashboard)
				.List()
				.Select((x, i) => new NameId(defaultWorkspaceName + (i > 0 ? " (" + i + ")" : ""), x.Id))
				.ToList();


			custom.AddRange(originals);

			return new WorkspaceDropdownVM() {
				AllMeetings = l10s,
				CustomWorkspaces = custom,
				DefaultWorkspaceId = allDashboards.FirstOrDefault(x => x.PrimaryDashboard).NotNull(x => x.Id),
				PrimaryWorkspace = primaryDash
			};
		}

		public static async Task SetHomeWorkspace(UserOrganizationModel caller, long userId, DashboardType type, long dashboardId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					await SetHomeWorkspace(s, perms, userId, type, dashboardId);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task SetHomeWorkspace(ISession s, PermissionsUtility perms, long userId, DashboardType modelType, long modelId) {
			perms.ViewDashboard(modelType, modelId);
			perms.Self(userId);

			var user = s.Get<UserOrganizationModel>(userId);
			user.PrimaryWorkspace = new UserOrganizationModel.PrimaryWorkspaceModel() {
				WorkspaceId = modelId,
				Type = modelType,
			};
			s.Update(user);
		}

		public static List<Dashboard> GetDashboardsForUser(ISession s, PermissionsUtility perms, long userId) {
			var user = s.Get<UserOrganizationModel>(userId);
			if (user == null || user.User == null) {
				throw new PermissionsException("User does not exist.");
			}

			perms.ViewDashboardForUser(user.User.Id);
			return s.QueryOver<Dashboard>().Where(x => x.DeleteTime == null && x.ForUser.Id == user.User.Id).List().ToList();

		}

		public static Dashboard GetPrimaryDashboardForUser(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetPrimaryDashboardForUser(s, caller, userId);
				}
			}
		}

		public static Dashboard GetPrimaryDashboardForUser(ISession s, UserOrganizationModel caller, long userId) {
			var user = s.Get<UserOrganizationModel>(userId);
			if (user == null || user.User == null) {
				throw new PermissionsException("User does not exist.");
			}

			PermissionsUtility.Create(s, caller).ViewDashboardForUser(user.User.Id);
			return s.QueryOver<Dashboard>()
				.Where(x => x.DeleteTime == null && x.ForUser.Id == user.User.Id && x.PrimaryDashboard)
				.OrderBy(x => x.CreateTime).Desc
				.Take(1).SingleOrDefault();
		}

		public static Dashboard CreateDashboard(UserOrganizationModel caller, string title, bool primary, bool defaultDashboard = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					if (caller.User == null) {
						throw new PermissionsException("User does not exist.");
					}

					Dashboard dash = CreateDashboard(s, caller, title, primary, defaultDashboard);

					tx.Commit();
					s.Flush();
					return dash;
				}
			}
		}

		public static Dashboard CreateDashboard(ISession s, UserOrganizationModel caller, string title, bool primary, bool defaultDashboard) {
			if (primary) {
				var existing = s.QueryOver<Dashboard>().Where(x => x.DeleteTime == null && x.ForUser.Id == caller.User.Id && x.PrimaryDashboard).List();
				foreach (var e in existing) {
					e.PrimaryDashboard = false;
					s.Update(e);
				}
			} else {
				//If this the first one, then override primary to true
				primary = (!s.QueryOver<Dashboard>().Where(x => x.DeleteTime == null && x.ForUser.Id == caller.User.Id).Select(x => x.Id).List<long>().Any());
			}

			var dash = new Dashboard() {
				ForUser = caller.User,
				Title = title,
				PrimaryDashboard = primary,
				UnitsWide = 7,
			};
			s.Save(dash);

    		// await HooksRegistry.Each<IDashboadHook>((sess, x) => x.CreateDashboard(sess, caller, dash));

			if (defaultDashboard) {
				var perms = PermissionsUtility.Create(s, caller);

				if (caller.Organization.Settings.EnableCoreProcess) {
					CreateTile(s, perms, dash.Id, 1, 10, 3, 12, "/TileData/UserTodo2", "To-dos", TileType.Todo);
					CreateTile(s, perms, dash.Id, 4, 10, 3, 12, "/TileData/UserRock2", "Goals", TileType.Rocks);

				} else {
					CreateTile(s, perms, dash.Id, 1, 10, 3, 12, "/TileData/UserTodo2", "To-dos", TileType.Todo);
					CreateTile(s, perms, dash.Id, 4, 10, 3, 12, "/TileData/UserRock2", "Goals", TileType.Rocks);
				}
				CreateTile(s, perms, dash.Id, 1, 0, 6, 10, "/TileData/UserScorecard2", "Metrics", TileType.Scorecard);
				CreateTile(s, perms, dash.Id, 0, 13, 1, 9, "/TileData/OrganizationValues", "Core Values", TileType.Values);
				CreateTile(s, perms, dash.Id, 0, 7, 1, 6, "/TileData/FAQTips", "FAQ Guide", TileType.FAQGuide);
				CreateTile(s, perms, dash.Id, 0, 0, 1, 7, "/TileData/UserProfile2", "Profile", TileType.Profile);

			}

			return dash;
		}

		public static Dashboard GetDashboard(UserOrganizationModel caller, long dashboardId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetDashboard(s, perms, dashboardId);
				}
			}
		}

		private static Dashboard GetDashboard(ISession s, PermissionsUtility perms, long dashboardId) {
			var dash = s.Get<Dashboard>(dashboardId);
			if (dash == null) {
				return null;
			}

			perms.ViewDashboardForUser(dash.ForUser.Id);
			return dash;
		}

		public static TileModel CreateTile(ISession s, PermissionsUtility perms, long dashboardId, int x, int y, int w, int h, string dataUrl, string title, TileType type, string keyId = null, string v3Positioning = null, string v3StatsFiltering = null) {
			perms.EditDashboard(DashboardType.Standard, dashboardId);
			if (type == TileType.Invalid) {
				throw new PermissionsException("Invalid tile type");
			}

			var uri = new Uri(dataUrl, UriKind.Relative);
			if (uri.IsAbsoluteUri) {
				throw new PermissionsException("Data url must be relative");
			}

			var dashboard = s.Get<Dashboard>(dashboardId);

			var tile = (new TileModel() {
				Dashboard = dashboard,
				DataUrl = dataUrl,
				ForUser = dashboard.ForUser,
				Height = h,
				Width = w,
				X = x,
				Y = y,
				Type = type,
				Title = title,
				KeyId = keyId,
        V3Positioning = v3Positioning,
        V3StatsFiltering = v3StatsFiltering,
			});

			s.Save(tile);

      		// await HooksRegistry.Each<IDashboardHook>((sess, x) => x.CreateTitle(sess, title, null));

			return tile;
		}

		public static TileModel CreateTile(UserOrganizationModel caller, long dashboardId, int w, int h, int x, int y, string dataUrl, string title, TileType type, string keyId = null, string v3Positioning = null, string v3StatsFiltering = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var perms = PermissionsUtility.Create(s, caller);
					var tile = CreateTile(s, perms, dashboardId, x, y, w, h, dataUrl, title, type, keyId, v3Positioning, v3StatsFiltering);
					tx.Commit();
					s.Flush();
					return tile;
				}
			}
		}

    public static MeetingTileModel CreateMeetingTile(UserOrganizationModel caller, string title, TileType type, string keyId, string v3Positioning, string v3StatsFiltering)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          var tile = CreateMeetingTile(s, perms, title, type, keyId, v3Positioning, v3StatsFiltering);
          tx.Commit();
          s.Flush();
          return tile;
        }
      }
    }

    public static MeetingTileModel CreateMeetingTile(ISession s, PermissionsUtility perms, string title, TileType type, string keyId, string v3Positioning, string v3StatsFiltering)
    {
      perms.ViewDashboardForUser(perms.GetCaller().User.Id);
      if (type == TileType.Invalid)
      {
        throw new PermissionsException("Invalid tile type");
      }

      var tile = (new MeetingTileModel()
      {
        ForUser = perms.GetCaller().User,
        Type = type,
        Title = title,
        KeyId = keyId,
        V3Positioning = v3Positioning,
        V3StatsFiltering = v3StatsFiltering,
      });

      s.Save(tile);

      return tile;
    }

    public static MeetingTileModel EditMeetingTile(ISession s, PermissionsUtility perms, long tileId, string title, TileType? type, string keyId, string v3Positioning, string v3StatsFiltering) {
      var tile = s.Get<MeetingTileModel>(tileId);

      tile.Title = title ?? tile.Title;
      tile.Type = type ?? tile.Type;
      tile.KeyId = keyId ?? tile.KeyId;
      tile.V3Positioning = v3Positioning ?? tile.V3Positioning;
      tile.V3StatsFiltering = v3StatsFiltering ?? tile.V3StatsFiltering;


      s.Update(tile);

      // await HooksRegistry.Each<IDashboardHook>((sess, x) => x.UpdateTitle(sess, title, null));

      return tile;
    }


    public static TileModel EditTile(ISession s, PermissionsUtility perms, long tileId, int? w = null, int? h = null, int? x = null, int? y = null, bool? hidden = null, string dataUrl = null, string title = null, string v3Positioning = null) {
			var tile = s.Get<TileModel>(tileId);

			tile.Height = h ?? tile.Height;
			tile.Width = w ?? tile.Width;
			tile.X = x ?? tile.X;
			tile.Y = y ?? tile.Y;
			tile.Hidden = hidden ?? tile.Hidden;
			tile.Title = title ?? tile.Title;
      tile.V3Positioning = v3Positioning ?? tile.V3Positioning;

			if (dataUrl != null) {
				//Ensure relative
				var uri = new Uri(dataUrl, UriKind.Relative);
				if (uri.IsAbsoluteUri) {
					throw new PermissionsException("Data url must be relative.");
				}

				tile.DataUrl = dataUrl;
			}

			s.Update(tile);

      		// await HooksRegistry.Each<IDashboardHook>((sess, x) => x.UpdateTitle(sess, title, null));

			return tile;
		}

    public static TileModel EditTilePositionsV3(UserOrganizationModel caller, long tileId, string positions)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {

          var perms = PermissionsUtility.Create(s, caller).EditTile(tileId);

          var tile = s.Get<TileModel>(tileId);

          tile.V3Positioning = positions;

          s.Update(tile);
          tx.Commit();
          s.Flush();
          // await HooksRegistry.Each<IDashboardHook>((sess, x) => x.UpdateTitle(sess, title, null));

          return tile;
        }
      }
    }

    public static TileModel EditTile(UserOrganizationModel caller, long tileId, int? h = null, int? w = null, int? x = null, int? y = null, bool? hidden = null, string dataUrl = null, string title = null, string v3Positioning = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).EditTile(tileId);

					var o = EditTile(s, perms, tileId, w, h, x, y, hidden, dataUrl, title, v3Positioning);

					tx.Commit();
					s.Flush();
					return o;
				}
			}
		}

    public static MeetingTileModel EditMeetingTile(UserOrganizationModel caller, long tileId, string title = null, TileType? type = null, string keyId = null, string v3Positioning = null, string v3StatsFiltering = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);

          var o = EditMeetingTile(s, perms, tileId, title, type, keyId, v3Positioning, v3StatsFiltering);

          tx.Commit();
          s.Flush();
          return o;
        }
      }
    }

    public static void EditTiles(UserOrganizationModel caller, long dashboardId, IEnumerable<TileVM> model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).EditDashboard(DashboardType.Standard, dashboardId);

					var editIds = model.Select(x => x.id).ToList();

					var old = s.QueryOver<TileModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(editIds).List().ToList();

					if (!SetUtility.AddRemove(editIds, old.Select(x => x.Id)).AreSame()) {
						throw new PermissionsException("You do not have access to edit some tiles.");
					}

					if (old.Any(x => x.Dashboard.Id != dashboardId)) {
						throw new PermissionsException("You do not have access to edit this dashboard.");
					}

					foreach (var o in old) {
						var found = model.First(x => x.id == o.Id);
						o.X = found.x;
						o.Y = found.y;
						o.Height = found.h;
						o.Width = found.w;
						s.Update(o);

            			// await HooksRegistry.Each<IDashboardHook>((sess, x) => x.UpdateTitle(sess, o, null));
					}


					tx.Commit();
					s.Flush();
				}
			}
		}

    public static async void EditWorkspacePersonalTile(UserOrganizationModel caller, WorkspaceTileNodeEditModel model)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var personalTile = s.Get<TileModel>(model.Id);
          long recurrenceId = -1;
          long.TryParse(personalTile.KeyId, out recurrenceId);
          WorkspaceTileQueryModel tile = new WorkspaceTileQueryModel()
          {
            Id = personalTile.Id,
            MeetingId = recurrenceId,
            TileType = model.Type == null ? personalTile.Type.Transform(personalTile.DataUrl).ToString() : model.Type,
            WorkspaceId = personalTile.Dashboard.Id,
            tileSettings = string.IsNullOrEmpty(personalTile.V3StatsFiltering) ? WorkspaceTransformers.GetDefaultFilterSettings() : JsonSerializer.Deserialize<WorkspaceStatsTileQueryModel>(personalTile.V3StatsFiltering),
            positions = string.IsNullOrEmpty(personalTile.V3Positioning) ? WorkspaceTransformers.GetDefaultPositionSettings() : JsonSerializer.Deserialize<WorkspaceTilePositionQueryModel>(personalTile.V3Positioning),
          };

          bool isNewData = tile.tileSettings == null || tile.positions == null;

          if (model.TileSettings != null)
          {
            tile.tileSettings = model.TileSettings;
          }

          if (model.Archived.HasValue)
          {
            personalTile.Hidden = model.Archived.Value;
            if (model.Archived.Value)
            {
              await HooksRegistry.Each<IWorkspaceTileHook>((ses, x) => x.RemoveWorkspaceTile(ses, caller, tile, tile.WorkspaceId));
            }
          }
          personalTile.V3StatsFiltering = JsonSerializer.Serialize(tile.tileSettings);
          personalTile.V3Positioning = JsonSerializer.Serialize(tile.positions);

          if (string.IsNullOrEmpty(personalTile.V3Positioning))
          {
            long i = recurrenceId;
          }

          s.Update(personalTile);
          tx.Commit();
          s.Flush();

          WorkspaceTileQueryModel transformedTile = personalTile.Transform(caller);
          await HooksRegistry.Each<IWorkspaceTileHook>((ses, x) => x.UpdateWorkspaceTile(ses, caller, transformedTile));
          if (isNewData)
          {
            await HooksRegistry.Each<IWorkspaceTileHook>((ses, x) => x.InsertWorkspaceTile(ses, caller, transformedTile, personalTile.Dashboard.TransformDashboard()));
          }
        }
      }
    }

    public static DashboardAndTiles GetTilesAndDashboard(UserOrganizationModel caller, long dashboardId) {
			List<TileModel> tiles;
			Dashboard dash;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewDashboard(DashboardType.Standard, dashboardId);
					tiles = GetTiles(s, dashboardId);
					dash = GetDashboard(s, perms, dashboardId);

				}
			}
			return new DashboardAndTiles(dash, dash.UnitsWide ?? 14) {
				Tiles = tiles,
			};
		}

		public static List<TileModel> GetTiles(ISession s, long dashboardId) {
			return s.QueryOver<TileModel>()
				.Where(x => x.DeleteTime == null && x.Dashboard.Id == dashboardId && x.Hidden == false)
				.List().OrderBy(x => x.Y).ThenBy(x => x.X).ToList();
		}

		public static void RenameDashboard(UserOrganizationModel caller, long dashboardId, string title) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditDashboard(DashboardType.Standard, dashboardId);
					var d = s.Get<Dashboard>(dashboardId);
					d.Title = title;
					s.Update(d);

          			// await HooksRegistry.Each<IDashboardHook>((sess, x) => x.UpdateDashboard(sess, caller, d, null));

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task DeleteDashboard(UserOrganizationModel caller, long dashboardId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditDashboard(DashboardType.Standard, dashboardId);
					var d = s.Get<Dashboard>(dashboardId);
					d.DeleteTime = DateTime.UtcNow;
					s.Update(d);

          await HooksRegistry.Each<IDashboardHook>((sess, x) => x.DeleteDashboard(s, caller, d));

					tx.Commit();
					s.Flush();
				}
			}
		}


		public class DashboardAndTiles {
			public Dashboard Dashboard { get; set; }
			public List<TileModel> Tiles { get; set; }
			public int UnitsWide { get; set; }
			public DashboardAndTiles(Dashboard d, int unitsWide) {
				Dashboard = d;
				Tiles = new List<TileModel>();
				UnitsWide = unitsWide;
			}
		}


		public static DashboardAndTiles GenerateDashboard(UserOrganizationModel caller, long id, DashboardType type, int? width) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					switch (type) {
						case DashboardType.L10:
							return GenerateL10Dashboard(s, perms, id, width);
						default:
							throw new ArgumentOutOfRangeException("DashboardType", "" + type);
					}
				}
			}
		}


		private static DashboardAndTiles GenerateL10Dashboard(ISession s, PermissionsUtility perms, long id, int? width) {
			perms.ViewL10Recurrence(id);
			var recur = s.Get<L10Recurrence>(id);
			var now = DateTime.UtcNow;

			var d = new Dashboard() {
				Id = -1,
				CreateTime = DateTime.UtcNow,
				Title = recur.Name ?? " Weekly Meeting Dashboard",

			};
			var o = new DashboardAndTiles(d, 6);

			var measurableRowCounts = L10Accessor.GetMeasurableCount(s, perms, id);
			//2 is for header and footer...
			var scorecardHeight = (int)Math.Ceiling(2.0 + 2.0 + Math.Ceiling((measurableRowCounts.Measurables) * 18.0 / 19.0 + 0.47) + Math.Round(measurableRowCounts.Dividers / 5.0));
			var scorecardCount = (double)scorecardHeight / (double)TILE_HEIGHT;

			if (measurableRowCounts.Measurables == 0) {
				scorecardHeight = 2 + 4;
			}

			//Goals, todos, issues
			var nonScorecardTileHeight = (int)Math.Max(3 * TILE_HEIGHT, Math.Ceiling((5.0 - scorecardCount) * TILE_HEIGHT));
			var issueTileHeight = (int)Math.Ceiling(0.5 * nonScorecardTileHeight);

			width = Math.Max(1156, width ?? 1156);

			int w = Math.Min(4, (int)Math.Floor(width.Value / 580.0 + 0.33));


			//						  x, y									w										h
			o.Tiles.Add(new TileModel(0, 0, w * 3, scorecardHeight, "Metrics", TileTypeBuilder.L10Scorecard(id), d, now, null, null, null));
			o.Tiles.Add(new TileModel(0, scorecardHeight, w, issueTileHeight, "Goals", TileTypeBuilder.L10Rocks(id), d, now, null, null, null));

			o.Tiles.Add(new TileModel(0, scorecardHeight + issueTileHeight, w, nonScorecardTileHeight - issueTileHeight, "Stats", TileTypeBuilder.L10Stats(id), d, now, null, null, null));

			o.Tiles.Add(new TileModel(w, scorecardHeight, w, nonScorecardTileHeight, "To-dos", TileTypeBuilder.L10Todos(id), d, now, null, null, null));
			o.Tiles.Add(new TileModel(w * 2, scorecardHeight, w, issueTileHeight, "Issues", TileTypeBuilder.L10Issues(id), d, now, null, null, null));
			o.Tiles.Add(new TileModel(w * 2, scorecardHeight + issueTileHeight, w, nonScorecardTileHeight - issueTileHeight, "Headlines", TileTypeBuilder.L10PeopleHeadlines(id), d, now, null, null, null));


			return o;
		}

		public static UserOrganizationModel.PrimaryWorkspaceModel GetHomeDashboardForUser(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);

					var user = s.Get<UserOrganizationModel>(userId);

					if (user.PrimaryWorkspace != null) {

						// User has been removed from the meeting that was their primary workspace. Revert them to default
						if (!perms.IsPermitted(x => x.ViewDashboard(user.PrimaryWorkspace.Type, user.PrimaryWorkspace.WorkspaceId))) {
							user.PrimaryWorkspace = null;
							s.Update(user);

              // TODO: Send notification
						}
					}

					if (user.PrimaryWorkspace == null) {
						var primary = DashboardAccessor.GetPrimaryDashboardForUser(s, caller, userId);
						if (primary == null) {
							primary = DashboardAccessor.CreateDashboard(s, caller, null, false, true);
						}

						user.PrimaryWorkspace = new UserOrganizationModel.PrimaryWorkspaceModel() {
							Type = DashboardType.Standard,
							WorkspaceId = primary.Id,
						};

					}

					tx.Commit();
					s.Flush();
					return user.PrimaryWorkspace;
				}
			}
		}
	}
}
