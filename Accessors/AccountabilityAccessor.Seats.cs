using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Askables;
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
using NHibernate.Criterion;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;

namespace RadialReview.Accessors {
	public partial class AccountabilityAccessor : BaseAccessor {


		public static AccountabilityNode GetNodeById(UserOrganizationModel caller, long seatId, bool checkDeleted = true)
		{
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetNodeById(s, perms, seatId, checkDeleted);
				}
			}
		}
		public static AccountabilityNode GetNodeById(ISession s, PermissionsUtility perms, long seatId, bool checkDeleted = true) {
			var node = s.Get<AccountabilityNode>(seatId);

			if (node.DeleteTime != null && checkDeleted) {
				throw new PermissionsException("Seat is not accessible.");
			}

			perms.CanView(ResourceType.AccountabilityHierarchy, node.AccountabilityChartId);

			return node;
		}

		[Todo]
		public static async Task UpdateAccountabilityNode(ISession s, RealTimeUtility rt, PermissionsUtility perms, long nodeId, string positionName, List<long> userIds, UserCacheUpdater usersToUpdate) {
			perms.ManagesAccountabilityNodeOrSelf(nodeId);
			var now = DateTime.UtcNow;
			var node = s.Get<AccountabilityNode>(nodeId);

			if (positionName != null) {
				UpdatePosition_Unsafe(s, rt, perms, nodeId, positionName, now, usersToUpdate);
			}

			if (userIds != null) {
				await SetUsers(s, rt, perms, node.Id, userIds, usersToUpdate);
			}
		}


		public static async Task SwapOrder(UserOrganizationModel caller, long nodeId, int oldOrder, int newOrder, string connectionId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await using (var rt = RealTimeUtility.Create(connectionId)) {
						var perms = PermissionsUtility.Create(s, caller);
						perms.ManagesAccountabilityNodeOrSelf(nodeId);

						var node = s.Get<AccountabilityNode>(nodeId);
						var nodes = s.QueryOver<AccountabilityNode>()
							.Where(x => x.DeleteTime == null && x.ParentNodeId == node.ParentNodeId && (x.Ordering == newOrder || x.Ordering == oldOrder))
							.List().ToList();

						if (!(nodes.Any(x => x.Ordering == oldOrder) && nodes.Any(x => x.Ordering == newOrder))) {
							throw new PermissionsException("Selected seat not found");
						}

						foreach (var n in nodes) {
							if (n.Ordering == oldOrder) {
								n.Ordering = newOrder;
								s.Update(n);
							} else if (n.Ordering == newOrder) {
								n.Ordering = oldOrder;
								s.Update(n);
							}

							rt.UpdateOrganization(node.OrganizationId).Update(
								new AngularAccountabilityNode(n.Id) {
									order = n.Ordering
								}
							);
						}

						tx.Commit();
						s.Flush();
					}
				}
			}
		}
		[Todo]
		public static async Task SwapParents(UserOrganizationModel caller, long nodeId, long newParentId, string connectionId = null) {
			using (var usersToUpdate = new UserCacheUpdater()) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var perms = PermissionsUtility.Create(s, caller);
						var ordering = 0;
						var node = s.Get<AccountabilityNode>(nodeId);
						if (node == null)
							throw new PermissionsException("Node does not exist");
						perms.ManagesAccountabilityNodeOrSelf(node.Id)
							.ManagesAccountabilityNodeOrSelf(newParentId);

						var newParent = s.Get<AccountabilityNode>(newParentId);

						if (node.AccountabilityChartId != newParent.AccountabilityChartId)
							throw new PermissionsException("Nodes are not on the same chart.");

						if (node.ParentNodeId == null)
							throw new PermissionsException("Cannot move the root node.");

						var oldParentId = node.ParentNodeId;
						node.ParentNodeId = newParentId;

						var max = s.QueryOver<AccountabilityNode>().Where(x => x.ParentNodeId == newParentId && x.DeleteTime == null).Select(x => x.Ordering).List<int?>().Max();
						ordering = 1 + max ?? 0;
						node.Ordering = ordering;
						s.Update(node);

						var nodes = s.QueryOver<AccountabilityNode>().Where(x => x.AccountabilityChartId == node.AccountabilityChartId && x.DeleteTime == null)
							.Select(x => x.Id, x => x.ParentNodeId)
							.List<object[]>()
							.Select(x => new {
								nodeId = (long)x[0],
								parentId = (long?)x[1],
							}).ToList();

						if (GraphUtility.HasCircularDependency(nodes, x => x.nodeId, x => x.parentId)) {
							throw new PermissionsException("A circular dependancy was found. Node cannot be a parent of itself.");
						}
						if (oldParentId != newParentId) {
							var now = DateTime.UtcNow;
              AccountabilityNode oldParentNode = null;

              if (oldParentId != null) {
								oldParentNode = s.Get<AccountabilityNode>(oldParentId.Value);
								DeepAccessor.Unsafe.Actions.Remove(s, oldParentNode, node, now);

								//REMOVE MANAGER
								if (oldParentNode.GetUsers(s).Any() && node.GetUsers(s).Any()) {
									foreach (var op in oldParentNode.GetUsers(s)) {
										var founds = s.QueryOver<ManagerDuration>()
														.Where(x => x.DeleteTime == null && x.ManagerId == op.Id)
														.WhereRestrictionOn(x => x.SubordinateId).IsIn(node.GetUsers(s).Select(x => x.Id).ToList())
														.List().ToList();
										foreach (var f in founds.GroupBy(x => x.SubordinateId)) {
											//only one of each child user
											var found = f.First();
											found.DeleteTime = now;
											s.Update(found);
											usersToUpdate.Add(op.Id);
										}
									}
								}

								foreach (var op in oldParentNode.GetUsers(s)) {
									if (op.ManagerAtOrganization && !DeepAccessor.Permissions.HasChildren(s, oldParentNode.Id)) {
										await UserAccessor.EditUserPermissionLevel(s, perms, op.Id, false);
										op.ManagerAtOrganization = false;
									}
								}
							}

							var newParentNode = s.Get<AccountabilityNode>(newParentId);
							DeepAccessor.Unsafe.Actions.Add(s, newParentNode, node, node.OrganizationId, now);

							foreach (var np in newParent.GetUsers(s)) {
								if (!np.ManagerAtOrganization) {
									await UserAccessor.EditUserPermissionLevel(s, perms, np.Id, true);
									np.ManagerAtOrganization = true;
								}
							}

							//ADD MANAGER
							foreach (var nn in node.GetUsers(s)) {
								foreach (var pn in newParentNode.GetUsers(s)) {
									var md = new ManagerDuration() {
										ManagerId = pn.Id,
										Manager = s.Load<UserOrganizationModel>(pn.Id),
										SubordinateId = nn.Id,
										Subordinate = s.Load<UserOrganizationModel>(nn.Id),
										PromotedBy = perms.GetCaller().Id,
										CreateTime = now
									};
									s.Save(md);

									usersToUpdate.Add(pn.Id);

								}
							}

							usersToUpdate.AddRange(node.GetUsers(s));

							await using (var rt = RealTimeUtility.Create(connectionId)) {
								var orgHub = rt.UpdateOrganization(node.OrganizationId);  //skips updating self
								orgHub.Update(new AngularAccountabilityNode(newParentId) {
									children = AngularList.CreateFrom(AngularListType.Add, new AngularAccountabilityNode(node.Id) {
										order = ordering
									})
								});
								if (oldParentId != null) {
									orgHub.Update(new AngularAccountabilityNode(oldParentId.Value) {
										children = AngularList.CreateFrom(AngularListType.Remove, new AngularAccountabilityNode(node.Id))
									});
								}
							}
							await using (var rt = RealTimeUtility.Create()) {
								var orgHubMe = rt.UpdateOrganization(node.OrganizationId); //updates self
								orgHubMe.Update(new AngularAccountabilityNode(node.Id) {
									order = ordering
								});
							}

              if (oldParentNode != null && newParentNode != null)
                await HooksRegistry.Each<IOrgChartSeatHook>((ses, x) => x.AttachOrDetachOrgChartSeatSupervisor(ses, perms.GetCaller(), oldParentNode, newParentNode));

            } else {
							await using (var rt = RealTimeUtility.Create()) {
								var orgHub = rt.UpdateOrganization(node.OrganizationId); //updates self also
								orgHub.Update(new AngularAccountabilityNode(node.Id) {
									order = ordering
								});
							}
						}
						tx.Commit();
						s.Flush();
					}
				}
			}

		}
		public static async Task<AccountabilityNode> AppendNode(ISession s, PermissionsUtility perms, RealTimeUtility rt, long parentNodeId, UserCacheUpdater usersToUpdate, long? rolesGroupId = null, List<long> userIds = null, string positionName = null, List<string> rolesToInclude = null) {
			userIds = userIds ?? new List<long>();
			return await AppendNode(s, perms, rt, parentNodeId, rolesGroupId, userIds, false, positionName, rolesToInclude, usersToUpdate);
		}

		[Obsolete("Use the other AppendNode")]
		[Todo]
		public static async Task<AccountabilityNode> AppendNode(ISession s, PermissionsUtility perms, RealTimeUtility rt, long parentNodeId, long? rolesGroupId, List<long> userIds, bool skipAddManager, string positionName, List<string> rolesToInclude, UserCacheUpdater usersToUpdate) {
			rt = rt ?? RealTimeUtility.Create(false);

			var now = DateTime.UtcNow;
			var parent = s.Get<AccountabilityNode>(parentNodeId);
			if (parent == null)
				throw new PermissionsException("Parent does not exist");
			perms.ManagesAccountabilityNodeOrSelf(parent.Id);
			AccountabilityRolesGroup group = null;
			if (rolesGroupId != null) {
				group = s.Get<AccountabilityRolesGroup>(rolesGroupId);
				if (group.OrganizationId != parent.OrganizationId) {
					throw new PermissionsException("Could not access node");
				}
			} else {

				group = new AccountabilityRolesGroup() {
					OrganizationId = parent.OrganizationId,
					AccountabilityChartId = parent.AccountabilityChartId,
					CreateTime = now,
					_Roles = new List<RoleGroup>() {
						new RoleGroup(){
							Roles = new List<SimpleRole>(),

						}
					}
				};
				s.Save(group);
			}

			var users = parent.GetUsers(s);

			var parentUser = users.FirstOrDefault();
			if (parentUser != null && !parentUser.ManagerAtOrganization) {
				await UserAccessor.EditUserPermissionLevel(s, perms, parentUser.Id, true);
				parentUser.ManagerAtOrganization = true;
			}


			var max = s.QueryOver<AccountabilityNode>()
							.Where(x => x.ParentNodeId == parentNodeId && x.DeleteTime == null)
							.Select(x => x.Ordering)
							.List<int?>().Max();

			var ordering = 1 + max ?? 0;

			var node = new AccountabilityNode() {
				OrganizationId = parent.OrganizationId,
				ParentNodeId = parentNodeId,
				ParentNode = parent,
				AccountabilityRolesGroupId = group.Id,
				AccountabilityRolesGroup = group,
				AccountabilityChartId = parent.AccountabilityChartId,
				CreateTime = now,
				Ordering = ordering,

			};
			s.Save(node);

			var roleGroup = new RoleGroup()
			{
				AttachType = AttachType.Node,
				AttachId = node.Id,
				Roles = new List<SimpleRole>()
			};

			try {
				if (rolesToInclude != null && rolesToInclude.Any()) {

					var maxOrderO = s.CreateCriteria<SimpleRole>()
						.Add(Expression.Eq(Projections.Property<SimpleRole>(x => x.DeleteTime), null))
						.Add(Expression.Eq(Projections.Property<SimpleRole>(x => x.NodeId), node.Id))
						.SetProjection(Projections.Max(Projections.Property<SimpleRole>(x => x.Ordering)), Projections.Count(Projections.Property<SimpleRole>(x => x.Ordering)))
						.UniqueResult<object[]>();

					var maxOrder = 0;
					var count = (int)maxOrderO[1];
					if (count != 0) {
						maxOrder = (int)maxOrderO[0] + 1;
					}

					foreach (var roleString in rolesToInclude) {

						var r = new SimpleRole() {
							CreateTime = DateTime.UtcNow,
							Name = roleString,
							NodeId = node.Id,
							OrgId = node.OrganizationId,
							AttachType_Deprecated = "Node",
							Ordering = maxOrder,
						};
						s.Save(r);
						await HooksRegistry.Each<ISimpleRoleHook>((ses, x) => x.CreateRole(ses, r.Id));
						maxOrder += 1;

						roleGroup.Roles.Add(r);

					}

				}
			} catch (Exception e) { }

			node.AccountabilityRolesGroup._Roles.Add(roleGroup);

			DeepAccessor.Unsafe.Actions.Add(s, parent, node, parent.OrganizationId, now, false);
			await SetUsers(s, rt, perms, node.Id, userIds, skipAddManager, false, now, usersToUpdate);

			if (positionName != null) {
				SetPosition(s, perms, rt, node.Id, positionName, usersToUpdate);
			}

			var updater = rt.UpdateOrganization(node.OrganizationId);

			updater.Update(new AngularAccountabilityChart(node.AccountabilityChartId) {
				ExpandNode = parentNodeId,
				CenterNode = node.Id,
				ShowNode = node.Id,
			}, insert: 0);


			updater.Update(new AngularAccountabilityNode(parentNodeId) {
				children = AngularList.CreateFrom(AngularListType.Add, new AngularAccountabilityNode(node, editable: true))
			}, insert: 1);


			rt.UpdateUsers(perms.GetCaller().Id)
				.Update(new AngularAccountabilityChart(node.AccountabilityChartId) {
					Selected = node.Id
				});

      return node;
		}

		public static async Task<AccountabilityNode> AppendNode(UserOrganizationModel caller, long parentNodeId, long? rolesGroupId = null, List<long> userIds = null, List<string> rolesToInclude = null, string positionName = null, bool isRootNode = false) {
			using (var usersToUpdate = new UserCacheUpdater()) {
				AccountabilityNode node;
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						await using (var rt = RealTimeUtility.Create()) {
							var perms = PermissionsUtility.Create(s, caller);
							node = await AppendNode(s, perms, rt, parentNodeId, usersToUpdate, rolesGroupId, userIds, rolesToInclude: rolesToInclude, positionName: positionName);

              await HooksRegistry.Each<IOrgChartSeatHook>((ses, x) => x.CreateOrgChartSeat(ses, perms.GetCaller(), node, isRootNode));

              tx.Commit();
							s.Flush();

						}
					}
				}

        return node;
			}
		}
		public static async Task CloneNode(UserOrganizationModel caller, long currentNodeId, long? rolesGroupId = null, List<long> userIds = null) {
			using (var usersToUpdate = new UserCacheUpdater()) {
				//create new node
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						await using (var rt = RealTimeUtility.Create()) {
							var perms = PermissionsUtility.Create(s, caller);
							var currentNode = s.Get<AccountabilityNode>(currentNodeId);
							long parentNodeId = (long)currentNode.ParentNodeId;
							var roles = await GetRolesForSeat(s, perms, currentNode.Id);
							var rolesList = roles.OrderBy(x => x.Ordering).Select(x => x.Name).ToList();
							var positionName = currentNode.GetPositionName();
							var node = await AppendNode(s, perms, rt, parentNodeId, usersToUpdate, rolesGroupId, userIds, positionName, rolesList);
							tx.Commit();
							s.Flush();

						}
					}

				}
			}

		}

		[Todo]
		public static async Task RemoveNode(UserOrganizationModel caller, long nodeId) {
			using (var usersToUpdate = new UserCacheUpdater()) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						await using (var do_not_use = RealTimeUtility.Create(false)) {
							var perms = PermissionsUtility.Create(s, caller);
							var node = s.Get<AccountabilityNode>(nodeId);
							if (node.DeleteTime != null)
								throw new PermissionsException("Node does not exist.");
							perms.ManagesAccountabilityNodeOrSelf(nodeId);
							var children = s.QueryOver<AccountabilityNode>().Where(x => x.ParentNodeId == nodeId && x.DeleteTime == null).RowCount();

							if (children > 0)
								throw new PermissionsException("Cannot delete a node with children.");
							//HEEYY!!!!! if you remove ^ condition, you must update manager status for node. You must also remove subordinates.

							if (node.ParentNodeId == null)
								throw new PermissionsException("Cannot delete the root node.");


							var now = DateTime.UtcNow;
							var didntHaveChildren = false;

							await SetUsers(s, do_not_use, perms, node.Id, new List<long>(), false, false, now, usersToUpdate);
							UpdatePosition_Unsafe(s, do_not_use, perms, node.Id, null, now, usersToUpdate, isDeleteSeat: true);

							DeepAccessor.Unsafe.Actions.RemoveAll(s, node, now);

							node.DeleteTime = now;
							s.Update(node);

							foreach (var pn in node.ParentNode.GetUsers(s)) {
								if (pn.ManagerAtOrganization) {
									if (!DeepAccessor.Users.HasChildren(s, perms, pn.Id)) {
										await UserAccessor.EditUserPermissionLevel(s, perms, pn.Id, false);
										pn.ManagerAtOrganization = false;
									}
								}
							}


							tx.Commit();
							s.Flush();

							await using (var rt = RealTimeUtility.Create()) {
								var orgHub = rt.UpdateOrganization(node.OrganizationId);
								orgHub.Update(new AngularAccountabilityChart(node.AccountabilityChartId) {
									ShowNode = nodeId
								});
								orgHub.Update(new AngularAccountabilityNode(node.ParentNodeId.Value) {
									children = AngularList.CreateFrom(AngularListType.Remove, new AngularAccountabilityNode(node.Id))
								});

								//Select it's parent.
                // now handled by frontend.
								//rt.UpdateUsers(perms.GetCaller().Id)
								//	.Update(new AngularAccountabilityChart(node.AccountabilityChartId) {
								//		Selected = node.ParentNodeId
								//	});
							}

              await HooksRegistry.Each<IOrgChartSeatHook>((ses, x) => x.DeleteOrgChartSeat(ses, node));
            }
					}
				}
			}
		}

		public static List<AngularAccountabilityNode> GetSeatsForUser(ISession s, PermissionsUtility perms, long userId) {
			perms.ViewUserOrganization(userId, false);
			var nodesForUser = DeepAccessor.Unsafe.Criterions.SelectNodeIdsGivenUser_Unsafe(userId);

			var rolesF = s.QueryOver<SimpleRole>()
				.Where(x => x.DeleteTime == null)
				.WithSubquery.WhereProperty(x => x.NodeId).In(nodesForUser)
				.Future();

			var nodesF = s.QueryOver<AccountabilityNode>()
				.Where(x => x.DeleteTime == null)
				.WithSubquery.WhereProperty(x => x.Id).In(nodesForUser)
				.Future();

			var roles = rolesF.ToList();
			var nodes = nodesF.ToList();

			return nodes.Select(n => new AngularAccountabilityNode(
				n.GetAccountabilityRolesGroupId(),
				n.Id,
				n.Ordering,
				n.GetPositionName(),
				null,
				null,
				roles.Where(x => x.NodeId == n.Id).ToList(),
				false,
				false
			)).ToList();
		}
	}
}
