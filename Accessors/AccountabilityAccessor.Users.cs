using NHibernate;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Enums;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static RadialReview.Models.PermItem;


namespace RadialReview.Accessors {
	public partial class AccountabilityAccessor : BaseAccessor {
		[Todo]
		public static List<long> GetNodeIdsForUser(ISession s, PermissionsUtility perms, long userId) {
			perms.ViewUserOrganization(userId, false);
			AccountabilityNode node = null;
			return s.QueryOver<AccountabilityNodeUserMap>()
				.JoinAlias(x => x.AccountabilityNode, () => node)
				.Where(x => x.DeleteTime == null && x.User.Id == userId && node.DeleteTime == null)
				.Select(x => x.AccountabilityNode.Id)
				.List<long>().ToList();
		}

		public static List<AccountabilityNode> GetNodesForUser(UserOrganizationModel caller, long userId) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetNodesForUser(s, perms, userId);
				}
			}
		}
		[Todo]
		public static List<AccountabilityNode> GetNodesForUser(ISession s, PermissionsUtility perms, long userId) {
			perms.ViewUserOrganization(userId, false);
			var items = s.QueryOver<AccountabilityNodeUserMap>()
				.Where(x => x.DeleteTime == null && x.User.Id == userId)
				.Fetch(x => x.AccountabilityNode).Eager
				.List().Select(x => x.AccountabilityNode)
				.Where(x => x.DeleteTime == null)
				.ToList();

			foreach (var i in items) {
				var a = i.AccountabilityRolesGroup.NotNull(x => x.PositionName);
			}
			return items;
		}

		[Todo("new")]
		public static async Task AddUserToNode(UserOrganizationModel caller, long nodeId, long userId, string connectionId = null) {
			using (var usersToUpdate = new UserCacheUpdater()) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						await using (var rt = RealTimeUtility.Create(connectionId)) {
							var perms = PermissionsUtility.Create(s, caller);
							await AddUserToNode(s, perms, rt, nodeId, userId, usersToUpdate);
							tx.Commit();
							s.Flush();
						}
					}
				}
			}
		}

		public static async Task AddUserToNode(ISession s, PermissionsUtility perms, RealTimeUtility rt, long nodeId, long userId, UserCacheUpdater usersToUpdate) {
			var node = s.Get<AccountabilityNode>(nodeId);
			var userIds = node.GetUsers(s).Select(x => x.Id).ToList();
			userIds.Add(userId);
			await SetUsers(s, rt, perms, nodeId, userIds, false, false, DateTime.UtcNow, usersToUpdate);
		}

		[Todo("new")]
		public static async Task RemoveUserFromNode(UserOrganizationModel caller, long nodeId, long userId, string connectionId = null) {
			using (var usersToUpdate = new UserCacheUpdater()) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						await using (var rt = RealTimeUtility.Create(connectionId)) {
							var perms = PermissionsUtility.Create(s, caller);
							var node = s.Get<AccountabilityNode>(nodeId);
							var userIds = node.GetUsers(s).Select(x => x.Id).ToList();
							if (userIds.RemoveAll(x => x == userId) > 0) {
								await SetUsers(s, rt, perms, nodeId, userIds, false, false, DateTime.UtcNow, usersToUpdate);
								tx.Commit();
								s.Flush();
							}
						}
					}
				}
			}
		}

		[Todo("new")]
		public static async Task RemoveAllUsersFromNode(UserOrganizationModel caller, long nodeId, string connectionId = null) {
			using (var usersToUpdate = new UserCacheUpdater()) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						await using (var rt = RealTimeUtility.Create(connectionId)) {
							var perms = PermissionsUtility.Create(s, caller);
							await SetUsers(s, rt, perms, nodeId, new List<long>(), false, false, DateTime.UtcNow, usersToUpdate);
							tx.Commit();
							s.Flush();
						}
					}
				}
			}
		}

		[Todo]
		public static async Task SetUsers(UserOrganizationModel caller, long nodeId, List<long> userIds, string connectionId = null) {
			using (var usersToUpdate = new UserCacheUpdater()) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						await using (var rt = RealTimeUtility.Create(connectionId)) {
							var perms = PermissionsUtility.Create(s, caller);
							await SetUsers(s, rt, perms, nodeId, userIds, usersToUpdate);
							tx.Commit();
							s.Flush();
						}
					}
				}
			}
		}

		[Todo]
		public static async Task SetUsers(ISession s, RealTimeUtility rt, PermissionsUtility perms, long nodeId, List<long> userIds, UserCacheUpdater usersToUpdate) {
			await SetUsers(s, rt, perms, nodeId, userIds, false, false, DateTime.UtcNow, usersToUpdate);
		}


		[Obsolete("Use the other SetUsers.")]
		[Todo]
		public static async Task SetUsers(ISession s, RealTimeUtility rt, PermissionsUtility perms, long nodeId, List<long> userIds, bool skipAddManger, bool skipPosition, DateTime now, UserCacheUpdater usersToUpdate) {

			if (userIds == null) {
				//should we throw an exception here?
				return;
			}
			userIds = userIds.Distinct().ToList();

			var updateUsers = new List<long>();

			var _n = s.Get<AccountabilityNode>(nodeId);
			var nId = nodeId;
			var nChartId = _n.AccountabilityChartId;
			var nOrganizationId = _n.OrganizationId;
			var nParentNode = _n.ParentNode;

			var nPositionName = _n.GetPositionName();
			var nAccountabilityRolesGroupId = _n.GetAccountabilityRolesGroupId();

			var oldUserIds = _n.GetUsers(s).Select(x => x.Id).ToList();

			var items = SetUtility.AddRemove(oldUserIds, userIds);
			var orgUpdater = rt.UpdateOrganization(nOrganizationId);

			foreach (var ui in items) {

				if (ui.Changed) {
					perms.Or(
						x => x.ManagesUserOrganization(ui.Item, true),
						x => x.ViewUserOrganization(ui.Item, false).CanEdit(ResourceType.AccountabilityHierarchy, nChartId)
					);
				}
				perms.ManagesAccountabilityNodeOrSelf(nodeId);
				if (ui.Changed) {

					//The old user
					if (ui.Removed) {
						//REMOVING USER FROM NODE

						//REMOVE MANAGER
						if (nParentNode != null && nParentNode.GetUsers(s).Any()) {
							var parentUserIds = nParentNode.GetUsers(s).Select(x => x.Id).ToList();
							var founds = s.QueryOver<ManagerDuration>()
								.Where(x => x.DeleteTime == null  && x.SubordinateId == ui.Item)
								.WhereRestrictionOn(x => x.ManagerId).IsIn(parentUserIds)
								.List()
								.Distinct(x => x.ManagerId) //just removing one.
								.ToList();

							foreach (var found in founds) {
								found.DeleteTime = now;
								s.Update(found);
							}

							foreach (var id in parentUserIds) {
								updateUsers.Add(id);
							}

						}

						//REMOVE SUBORDINATES
						var childUserIds = GetChildUserIds(s, nId);

						foreach (var cid in childUserIds) {
							//just remove one
							var found = s.QueryOver<ManagerDuration>()
											.Where(x => x.DeleteTime == null && x.ManagerId == ui.Item && x.SubordinateId == cid)
											.Take(1).SingleOrDefault();

							if (found != null) {
								found.DeleteTime = now;
								s.Update(found);
								updateUsers.Add(cid);
							}
						}


						if (ui.Removed) {//I think this if is unneeded.
										 //Update positions
							if (!skipPosition && nPositionName != null) {
								var pd = s.QueryOver<PositionDurationModel>().Where(x => x.DeleteTime == null && x.PositionName == nPositionName && x.UserId == ui.Item).Take(1).SingleOrDefault();
								if (pd != null) {
									pd.DeleteTime = now;
									pd.DeletedBy = perms.GetCaller().Id;
									s.Update(pd);
								}
							}
							updateUsers.Add(ui.Item);
						}


						//User is removed from updater below...
						orgUpdater.Update(new AngularAccountabilityGroup(nAccountabilityRolesGroupId) {
							RoleGroups = AngularList.CreateFrom(AngularListType.Remove, new AngularRoleGroup(new Attach(AttachType.User, ui.Item), null))
						}, x => x.ForceNoSkip());
						var oldUserId = ui.Item;

						var toDelete = s.QueryOver<AccountabilityNodeUserMap>()
							.Where(x => x.DeleteTime == null && x.AccountabilityNode.Id == nId && x.User.Id == ui.Item)
							.List().ToList();

						foreach (var d in toDelete) {
							d.DeleteTime = now;
							s.Update(d);
						}

						if (ui.Removed) {
							//Remove Manager status
							if (!DeepAccessor.Users.HasChildren(s, perms, ui.Item)) {
								await UserAccessor.EditUserPermissionLevel(s, perms, ui.Item, false);
								s.Flush();
								var u = s.Get<UserOrganizationModel>(ui.Item);
								u.ManagerAtOrganization = false;
								s.Update(u);
							}
						}
					}

					//The new user
					if (ui.Added) {
						//ADDING USER TO NODE

						s.Save(new AccountabilityNodeUserMap() {
							AccountabilityNode = s.Load<AccountabilityNode>(nodeId),
							AccountabilityNodeId = nodeId,
							OrgId = nOrganizationId,
							ChartId = nChartId,
							User = s.Load<UserOrganizationModel>(ui.Item),
							UserId = ui.Item
						});

						var user = s.Get<UserOrganizationModel>(ui.Item);

						if (DeepAccessor.Permissions.HasChildren(s, nId)) {
							//UPDATE MANAGER STATUS,
							if (!user.ManagerAtOrganization) {
								await UserAccessor.EditUserPermissionLevel(s, perms, ui.Item, true);
								user.ManagerAtOrganization = true;
							}
							//ADD SUBORDINATES
							if (!skipAddManger) {

								var childIds = GetChildUserIds(s, nId);
								foreach (var cid in childIds) {
									var md = new ManagerDuration() {
										ManagerId = ui.Item,
										Manager = s.Load<UserOrganizationModel>(ui.Item),
										SubordinateId = cid,
										Subordinate = s.Load<UserOrganizationModel>(cid),
										PromotedBy = perms.GetCaller().Id,
										CreateTime = now,

									};
									s.Save(md);
									updateUsers.Add(cid);
								}
							}
						}
						//ADD MANAGER
						if (nParentNode != null && !skipAddManger) {
							foreach (var parentUser in nParentNode.GetUsers(s)) {
								var md = new ManagerDuration() {
									ManagerId = parentUser.Id,
									Manager = s.Load<UserOrganizationModel>(parentUser.Id),
									SubordinateId = ui.Item,
									Subordinate = s.Load<UserOrganizationModel>(ui.Item),
									PromotedBy = perms.GetCaller().Id,
									CreateTime = now
								};
								s.Save(md);
								updateUsers.Add(parentUser.Id);
							}
						}

						//Update positions
						if (!skipPosition && nPositionName != null) {
							var pd = new PositionDurationModel() {
								UserId = ui.Item,
								CreateTime = now,
								PositionName = nPositionName,
								PromotedBy = perms.GetCaller().Id,
								OrganizationId = nOrganizationId,
							};
							s.Save(pd);
						}

						s.Flush();
						updateUsers.Add(ui.Item);
					}
				} else {
					//No change?
				}
				s.Flush();
			}

			if (items.Any(x => x.Changed)) {
				var newUsers = s.QueryOver<UserOrganizationModel>()
					.Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id)
					.IsIn(items.NewValues.ToArray())
					.List().ToList();

				orgUpdater.Update(new AngularAccountabilityNode(nId) {
					Users = AngularList.Create(AngularListType.ReplaceAll, newUsers.Select(x => AngularUser.CreateUser(x))),
				});
			}
			usersToUpdate.AddRange(updateUsers);

      await HooksRegistry.Each<IOrgChartSeatHook>((ses, x) => x.AttachOrgChartSeatUser(ses, perms.GetCaller(), _n, items.RemovedValues.ToArray(), items.AddedValues.ToArray()));
    }

		private static List<long> GetChildUserIds(ISession s, long nId) {
			var childNodeIds = s.QueryOver<AccountabilityNode>()
										.Where(x => x.DeleteTime == null && x.ParentNodeId == nId)
										.Select(x => x.Id)
										.List<long>().ToList();

			var childUserIds = s.QueryOver<AccountabilityNodeUserMap>()
				.Where(x => x.DeleteTime == null)
				.WhereRestrictionOn(x => x.AccountabilityNode.Id).IsIn(childNodeIds)
				.Select(x => x.User.Id)
				.List<long>().ToList();
			return childUserIds;
		}

	}
}
