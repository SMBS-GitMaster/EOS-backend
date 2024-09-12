using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Models.Accountability;
using FluentNHibernate.Mapping;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.UserModels;

namespace RadialReview.Accessors.Old {
	[Obsolete("DO NOT USE")]
	public class Old {

		public class NilObj {
		}
		public class DeepAccessor : BaseAccessor {
			#region Helpers
			public class Dive {
				public static List<AccountabilityNode> GetSubordinates(ISession s, AccountabilityNode manager, int levels = int.MaxValue) {
					var alreadyHit = new List<long>();
					return Children(s, manager, new List<String> { "" + manager.Id }, levels, 0, alreadyHit);
				}
				public static List<AccountabilityNode> GetSuperiors(ISession s, AccountabilityNode manager, int levels = int.MaxValue) {
					var alreadyHit = new List<long>();
					return Parents(s, manager, new List<String> { "" + manager.Id }, levels, 0, alreadyHit);
				}

				private static List<AccountabilityNode> Children(ISession s, AccountabilityNode parent, List<String> parents, int levels, int level, List<long> alreadyHit) {
					var children = new List<AccountabilityNode>();
					if (levels <= 0)
						return children;
					levels = levels - 1;

					parent = s.Get<AccountabilityNode>(parent.Id);


					children = s.QueryOver<AccountabilityNode>()
						.Where(x => x.DeleteTime == null && x.ParentNodeId == parent.Id)
						.List().ToList();


					if (!children.Any())
						return children;
					var iter = children.ToList();
					foreach (var c in iter) {
						if (!alreadyHit.Contains(c.Id)) {
							alreadyHit.Add(c.Id);
							children.Add(c);
							var copy = parents.Select(x => x).ToList();
							copy.Add("" + c.Id);
							children.AddRange(Children(s, c, copy, levels, level + 1, alreadyHit));
						}
					}
					return children;
				}

				private static List<AccountabilityNode> Parents(ISession s, AccountabilityNode child, List<String> children, int levels, int level, List<long> alreadyHit) {
					var parents = new List<AccountabilityNode>();
					if (levels <= 0)
						return parents;
					levels = levels - 1;
					child = s.Get<AccountabilityNode>(child.Id);

					if (child.ParentNode != null)
						parents.Add(child.ParentNode);



					if (parents.Count == 0)
						return parents;
					var c = child.ParentNode;
					if (!alreadyHit.Contains(c.Id)) {
						alreadyHit.Add(c.Id);
						parents.Add(c);
						var copy = children.Select(x => x).ToList();
						copy.Add("" + c.Id);
						parents.AddRange(Parents(s, c, copy, levels, level + 1, alreadyHit));
					}
					return parents;
				}
			}

			public class Tiny {

				private static Func<object[], TinyUser> Unpackage = new Func<object[], TinyUser>(x => {
					var fname = (string)x[0];
					var lname = (string)x[1];
					var email = (string)x[5];
					var uoId = (long)x[2];
					if (fname == null && lname == null) {
						fname = (string)x[3];
						lname = (string)x[4];
						email = (string)x[6];
					}
					return new TinyUser() {
						FirstName = fname,
						LastName = lname,
						Email = email,
						UserOrgId = uoId
					};
				});

				public static List<TinyUser> GetSubordinatesAndSelf(UserOrganizationModel caller, long userId, NilObj type = null) {
					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							var ids = Users.GetSubordinatesAndSelf(s, caller, userId, type).ToArray();

							TempUserModel tempUserAlias = null;
							UserOrganizationModel userOrgAlias = null;
							UserModel userAlias = null;

							return s.QueryOver<UserOrganizationModel>(() => userOrgAlias)
								.Left.JoinAlias(x => x.User, () => userAlias)
								.Left.JoinAlias(x => x.TempUser, () => tempUserAlias)
								.Where(x => x.DeleteTime == null)
								.WhereRestrictionOn(x => x.Id).IsIn(ids)
								.Select(x => userAlias.FirstName, x => userAlias.LastName, x => x.Id, x => tempUserAlias.FirstName, x => tempUserAlias.LastName, x => userAlias.UserName, x => tempUserAlias.Email)
								.List<object[]>()
								.Select(Unpackage)
								.ToList();

						}
					}
				}

			}


			public class Users {
				public class DeleteRecord {
					public virtual long Id { get; set; }
					//DeleteRecord created (user removed from node)
					public virtual DateTime Time { get; set; }
					public virtual long UserId { get; set; }
					public virtual long NodeId { get; set; }
					//DeleteRecord delete time
					public virtual DateTime? DeleteTime { get; set; }

					public class Map : ClassMap<DeleteRecord> {
						public Map() {
							Id(x => x.Id);
							Map(x => x.Time);
							Map(x => x.UserId);
							Map(x => x.NodeId);
							Map(x => x.DeleteTime);
						}
					}
				}


				public static void DeleteAll(ISession s, UserOrganizationModel forUser, DateTime? deleteTime = null) {
					var id = forUser.Id;
					var all = s.QueryOver<AccountabilityNode>().Where(x => x.DeprecatedUserId == id).List().ToList();
					var dt = deleteTime ?? DateTime.UtcNow;

					foreach (var a in all) {
						a.DeprecatedUserId = null;
						s.Update(a);

						s.Save(new DeleteRecord() {
							Time = dt,
							NodeId = a.Id,
							UserId = id
						});
					}
				}
				public static bool UndeleteAll(ISession s, UserOrganizationModel forUser, DateTime deleteTime, ref List<String> messages) {
					var id = forUser.Id;
					var all = s.QueryOver<DeleteRecord>().Where(x => x.Time == deleteTime && x.DeleteTime == null).List().ToList();
					if (messages == null)
						messages = new List<String>();
					var count = 0;
					var success = 0;
					var allSuccess = true;
					foreach (var a in all) {
						var node = s.Get<AccountabilityNode>(a.NodeId);
						if (node.DeprecatedUserId == null) {
							node.DeprecatedUserId = a.UserId;
							s.Update(node);
							success++;
						} else {
							messages.Add("Could not re-add to organizational chart. Accountability already occupied by " + node.DeprecatedUser.GetName() + ".");
							allSuccess = false;
						}
						count++;
					}
					messages.Add("Updated " + success + "/" + count + " accountabilities.");
					return allSuccess;
				}
				public static List<long> GetSubordinatesAndSelf(UserOrganizationModel caller, long userId, NilObj type = null, bool includeTempUsers = true) {
					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							return GetSubordinatesAndSelf(s, caller, userId, type, includeTempUsers: includeTempUsers);
						}
					}
				}

				public static List<UserOrganizationModel> GetSubordinatesAndSelfModels(ISession s, UserOrganizationModel caller, long userId, NilObj type = null) {
					var userIds = GetSubordinatesAndSelf(s, caller, userId, type);
					var users = s.QueryOver<UserOrganizationModel>()
						.WhereRestrictionOn(x => x.Id).IsIn(userIds.ToArray())
						.List().ToList();

					return users;
				}


				public static List<long> GetSubordinatesAndSelf(ISession s, UserOrganizationModel caller, long userId, NilObj type = null, bool includeTempUsers = true) {
					return GetSubordinatesAndSelf(s, PermissionsUtility.Create(s, caller), userId, type, includeTempUsers: includeTempUsers).ToList();
				}

				public static IEnumerable<long> GetSubordinatesAndSelf(ISession s, PermissionsUtility perms, long userId, NilObj type = null, bool excludeSelf = false, bool includeTempUsers = true) {
					var user = s.Get<UserOrganizationModel>(userId);
					if (user == null || user.DeleteTime != null)
						throw new PermissionsException("User (" + userId + ") does not exist.");

					perms.ViewOrganization(user.Organization.Id);
					var caller = perms.GetCaller();

					if (caller.Id != userId && !PermissionsUtility.IsAdmin(s, caller)) {
						AccountabilityNode parent = null;
						AccountabilityNode child1 = null;
						var found = s.QueryOver<DeepAccountability>().Where(x => x.DeleteTime == null)
									.JoinAlias(x => x.Parent, () => parent)
									.JoinAlias(x => x.Child, () => child1)
										.Where(x => parent.DeleteTime == null && child1.DeleteTime == null && parent.DeprecatedUserId == caller.Id && child1.DeprecatedUserId == userId)
										.Take(1)
										.SingleOrDefault();
						if (found == null)
							throw new PermissionsException("You don't have access to this user");
					}


					var allMyNodesQuery = QueryOver.Of<AccountabilityNode>()
														.Where(x => x.DeleteTime == null && x.OrganizationId == user.Organization.Id && x.DeprecatedUserId == userId)
														.Select(x => x.Id);


					AccountabilityNode child = null;
					var subordinateQueries = new List<IEnumerable<long>>();


					subordinateQueries.Add(
						s.QueryOver<DeepAccountability>()
							.Where(x => x.DeleteTime == null)
							.WithSubquery.WhereProperty(x => x.ParentId).In(allMyNodesQuery)
							.JoinAlias(x => x.Child, () => child)
								.Where(x => child.DeleteTime == null && child.DeprecatedUserId != null)
								.Select(x => child.DeprecatedUserId)
								.Future<long>()
					);

					if (type != null) {
						throw new NotImplementedException();

					}

					var subordinates = subordinateQueries.SelectMany(x => x);


					subordinates = subordinates.Union(userId.AsList());
					var results = subordinates.Distinct();

					if (!includeTempUsers) {
						results = s.QueryOver<UserOrganizationModel>()
							.WhereRestrictionOn(x => x.Id).IsIn(results.ToArray())
							.Where(x => x.User != null)
							.Select(x => x.Id)
							.List<long>().ToList();
					}

					return results;

				}


				public static bool HasChildren(ISession s, PermissionsUtility perms, long userId) {
					var nodeIds = AccountabilityAccessor.GetNodeIdsForUser(s, perms, userId);
					var futureVals = new List<IFutureValue<long>>();
					foreach (var nodeId in nodeIds) {
						futureVals.Add(s.QueryOver<DeepAccountability>().Where(x => x.DeleteTime == null && x.Links > 0 && x.ParentId == nodeId && x.ChildId != nodeId).ToRowCountInt64Query().FutureValue<long>());
					}
					foreach (var f in futureVals) {
						if (f.Value > 0)
							return true;
					}
					return false;
				}

				public static List<UserOrganizationModel> GetDirectReportsAndSelfModels(ISession s, PermissionsUtility perms, long userId) {
					var myNodeIds = AccountabilityAccessor.GetNodeIdsForUser(s, perms, userId);

					var users = myNodeIds.SelectMany(nodeId => DeepAccessor.GetDirectReportsAndSelf(s, perms, nodeId))
						.Distinct(x => x.Id)
						.Distinct(x => x.DeprecatedUserId)
						.Select(x => x.DeprecatedUser)
						.Where(x => x != null && x.DeleteTime == null)
						.ToList();

					//resolve all
					users.ForEach(x => x.GetName());
					return users;
				}
				/// <summary>
				/// 
				/// </summary>
				/// <param name="s"></param>
				/// <param name="perms"></param>
				/// <param name="managerId"></param>
				/// <param name="subordinateId"></param>
				/// <param name="allowDeletedSubordinateUser">for viewing user details page</param>
				/// <returns></returns>
				public static bool ManagesUser(ISession s, PermissionsUtility perms, long managerId, long subordinateId, bool allowDeletedSubordinateUser = false) {
					perms.ViewUserOrganization(managerId, false).ViewUserOrganization(subordinateId, false);
					var m = s.Get<UserOrganizationModel>(managerId);
					var sub = s.Get<UserOrganizationModel>(subordinateId);
					if (sub == null || (!allowDeletedSubordinateUser && sub.DeleteTime != null))
						throw new PermissionsException("Subordinate user (" + subordinateId + ") does not exist.");
					if (m == null || m.DeleteTime != null)
						throw new PermissionsException("Manager user (" + managerId + ") does not exist.");

					if (m.IsRadialAdmin)
						return true;
					if (m.ManagingOrganization && m.Organization.Id == sub.Organization.Id)
						return true;

					AccountabilityNode manager = null;
					AccountabilityNode subordinate = null;

					var found = s.QueryOver<DeepAccountability>()
						.Where(x => x.DeleteTime == null && x.Links > 0)
						.JoinAlias(x => x.Parent, () => manager)
						.JoinAlias(x => x.Child, () => subordinate)
							.Where(x => manager.DeleteTime == null && subordinate.DeleteTime == null && manager.DeprecatedUserId == managerId && subordinate.DeprecatedUserId == subordinateId)
							.Select(x => x.Id)
							.Take(1).SingleOrDefault<object>();


					return found != null;
				}

				public static bool ManagesUser(UserOrganizationModel caller, long managerId, long subordinateId) {
					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							var perms = PermissionsUtility.Create(s, caller);

							return ManagesUser(s, perms, managerId, subordinateId);
						}
					}
				}

				public static List<AccountabilityNode> GetNodesForUser(ISession s, PermissionsUtility perms, long userId) {
					perms.ViewUserOrganization(userId, false);

					var user = s.Get<UserOrganizationModel>(userId);
					if (user == null || user.DeleteTime != null)
						throw new PermissionsException("User (" + userId + ") does not exist.");

					return s.QueryOver<AccountabilityNode>().Where(x => x.DeleteTime == null && x.DeprecatedUserId == userId).List().ToList();

				}
			}
			#endregion
			[Obsolete("Did you mean DeepAccessor.Users.GetSubordinatesAndSelf")]
			public static List<long> GetChildrenAndSelf(UserOrganizationModel caller, long nodeId) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						return GetChildrenAndSelf(s, caller, nodeId);
					}
				}
			}

			[Obsolete("Did you mean DeepAccessor.Users.GetSubordinatesAndSelfModels")]
			public static List<AccountabilityNode> GetChildrenAndSelfModels(ISession s, UserOrganizationModel caller, long nodeId, bool allowAnyFromSameOrg = false) {
				var node = s.Get<AccountabilityNode>(nodeId);

				if (node == null || node.DeleteTime != null) {
					throw new PermissionsException("You don't have access to this user");
				}

				if (caller.Id != node.DeprecatedUserId && !PermissionsUtility.IsAdmin(s, caller)) {

					if (allowAnyFromSameOrg && node.OrganizationId == caller.Organization.Id) {
						//warning... this overrides the following permissions check...
					} else {
						AccountabilityNode parent = null;
						var found = s.QueryOver<DeepAccountability>().Where(x => x.DeleteTime == null && x.ChildId == nodeId)
									.JoinAlias(x => x.Parent, () => parent)
										.Where(x => parent.DeleteTime == null && parent.DeprecatedUserId == caller.Id)
										.Take(1)
										.SingleOrDefault();

						if (found == null)
							throw new PermissionsException("You don't have access to this user");
					}
				}
				var allPermissions = new List<long>() { nodeId };


				AccountabilityNode alias = null;

				var subordinates = s.QueryOver(() => alias)
									.WithSubquery.WhereExists(
										QueryOver.Of<DeepAccountability>()
											.Where(e => e.DeleteTime == null)
											.WhereRestrictionOn(x => x.ParentId).IsIn(allPermissions)
											.Where(d => d.ChildId == alias.Id)
											.Select(d => d.Id))
											.List().ToList();

				if (node != null && !subordinates.Any(x => x.Id == nodeId)) {
					subordinates.Add(node);
				}


				return subordinates;
			}


			[Obsolete("Did you mean DeepAccessor.Users.GetSubordinatesAndSelf")]
			public static List<long> GetChildrenAndSelf(ISession s, UserOrganizationModel caller, long nodeId, NilObj type = null) {
				var node = s.Get<AccountabilityNode>(nodeId);

				if (node == null || node.DeleteTime != null) {
					throw new PermissionsException("You don't have access to this user");
				}

				if (caller.Id != node.DeprecatedUserId && !PermissionsUtility.IsAdmin(s, caller)) {
					AccountabilityNode parent = null;
					var found = s.QueryOver<DeepAccountability>().Where(x => x.DeleteTime == null && x.ChildId == nodeId)
								.JoinAlias(x => x.Parent, () => parent)
									.Where(x => parent.DeleteTime == null && parent.DeprecatedUserId == caller.Id)
									.Take(1)
									.SingleOrDefault();
					if (found == null)
						throw new PermissionsException("You don't have access to this user");
				}
				var allPermitted = new List<long>() { nodeId };
				if (type != null) {
					throw new NotImplementedException();
				}


				var subordinates = s.QueryOver<DeepAccountability>()
										.Where(x => x.DeleteTime == null)
										.WhereRestrictionOn(x => x.ParentId).IsIn(allPermitted)
										.Select(x => x.ChildId)
										.List<long>()
										.ToList();
				subordinates.Add(nodeId);

				return subordinates.Distinct().ToList();
			}

			[Obsolete("Did you mean DeepAccessor.Users.GetDirectReportsAndSelfModels")]
			public static List<AccountabilityNode> GetDirectReportsAndSelf(ISession s, PermissionsUtility perms, long forNodeId) {
				var forNode = s.Get<AccountabilityNode>(forNodeId);
				perms.ViewHierarchy(forNode.AccountabilityChartId);

				var list = s.QueryOver<AccountabilityNode>()
					.Where(x => x.ParentNodeId == forNodeId && x.DeleteTime == null && x.AccountabilityChartId == forNode.AccountabilityChartId)
					.Fetch(x => x.DeprecatedUser).Eager
					.Fetch(x => x.DeprecatedUser.TempUser).Eager
					.Fetch(x => x.DeprecatedUser.Cache).Eager
					.Fetch(x => x.AccountabilityRolesGroup).Eager
					.Fetch(x => x.AccountabilityRolesGroup.DepricatedPosition).Eager
					.List().ToList();
				list.Insert(0, forNode);

				foreach (var i in list) {
					var a = i.DeprecatedUser.NotNull(x => x.GetName());
					var b = i.AccountabilityRolesGroup.NotNull(x => x.DepricatedPosition.GetName());
				}

				return list;
			}

			public static bool HasChildren(ISession s, long nodeId) {
				return s.QueryOver<DeepAccountability>()
							.Where(x => x.DeleteTime == null && x.Links > 0 && x.ParentId == nodeId && x.ChildId != nodeId)
							.RowCount() > 0;
			}

			public static bool ManagesNode(ISession s, PermissionsUtility perms, long managerUserId, long nodeId) {
				perms.ViewUserOrganization(managerUserId, false);
				var m = s.Get<UserOrganizationModel>(managerUserId);
				var node = s.Get<AccountabilityNode>(nodeId);

				var user = s.Get<UserOrganizationModel>(managerUserId);
				if (user == null || user.DeleteTime != null)
					throw new PermissionsException("User (" + managerUserId + ") does not exist.");

				if (m.IsRadialAdmin)
					return true;
				if (m.ManagingOrganization && m.Organization.Id == node.OrganizationId)
					return true;

				if (node.DeleteTime != null)
					throw new PermissionsException("Accountability node (" + nodeId + ") does not exist.");



				AccountabilityNode manager = null;

				var found = s.QueryOver<DeepAccountability>()
					.Where(x => x.DeleteTime == null && x.Links > 0)
					.JoinAlias(x => x.Parent, () => manager)
						.Where(x => manager.DeleteTime == null && manager.DeprecatedUserId == managerUserId && x.ChildId == nodeId)
						.Take(1)
						.SingleOrDefault();


				return found != null;
			}

			public static List<DeepAccountability> GetOrganizationMap(ISession s, PermissionsUtility perm, long organizationId) {
				perm.ViewOrganization(organizationId);
				return s.QueryOver<DeepAccountability>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId).List().ToList();
			}
			[Obsolete("Use UserAccessor.RemoveManager", false)]
			public static void Remove(ISession s, AccountabilityNode parent, AccountabilityNode child, DateTime now, bool ignoreCircular = false) {
				//Grab all subordinates' deep subordinates
				var allSuperiors = Dive.GetSuperiors(s, parent);
				allSuperiors.Add(parent);
				var allSubordinates = Dive.GetSubordinates(s, child);
				allSubordinates.Add(child);


				foreach (var SUP in allSuperiors) {
					var managerSubordinates = s.QueryOver<DeepAccountability>().Where(x => x.ParentId == SUP.Id).List().ToListAlive();

					foreach (var sub in allSubordinates) {
						var found = managerSubordinates.FirstOrDefault(x => x.ChildId == sub.Id && x.Links > 0);
						if (found == null) {
							log.Error("Manager link doesn't exist for orgId=(" + parent.OrganizationId + "). Advise that you run deep subordinate creation.");
						} else {
							found.Links -= 1;
							if (found.Links == 0)
								found.DeleteTime = now;
							if (found.Links < 0) {
								if (!ignoreCircular)
									throw new Exception("This shouldn't happen.");
							}
							s.Update(found);
						}
					}
				}
			}

			[Obsolete("Use UserAccessor.AddManager", false)]
			public static void Add(ISession s, AccountabilityNode manager, AccountabilityNode subordinate, long organizationId, DateTime now, bool ignoreCircular = false) {
				//Get **users** subordinates, make them deep subordinates of manager
				var allSubordinates = Dive.GetSubordinates(s, subordinate);
				allSubordinates.Add(subordinate);
				var allSuperiors = Dive.GetSuperiors(s, manager);
				allSuperiors.Add(manager);

				var allManagerSubordinates = s.QueryOver<DeepAccountability>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.ParentId).IsIn(allSuperiors.Select(x => x.Id).ToList())
					.List().ToList();


				//for manager and each of his superiors
				foreach (var SUP in allSuperiors) {
					var managerSubordinates = allManagerSubordinates.Where(x => x.ParentId == SUP.Id).ToList();

					foreach (var sub in allSubordinates) {
						if (sub.Id == SUP.Id && !ignoreCircular) {
							var mname = "" + manager.Id;
							var sname = "" + subordinate.Id;
							if (manager.DeprecatedUser != null)
								mname = manager.DeprecatedUser.GetName();
							if (subordinate.DeprecatedUser != null)
								sname = subordinate.DeprecatedUser.GetName();

							throw new PermissionsException("A circular dependency was found. " + mname + " cannot manage " + sname + " because " + mname + " is " + sname + "'s subordinate.");
						}

						var found = managerSubordinates.FirstOrDefault(x => x.ChildId == sub.Id);
						if (found == null) {
							found = new DeepAccountability() {
								CreateTime = now,
								ParentId = SUP.Id,
								Parent = s.Load<AccountabilityNode>(SUP.Id),
								ChildId = sub.Id,
								Child = s.Load<AccountabilityNode>(sub.Id),
								Links = 0,
								OrganizationId = organizationId
							};
						}
						found.Links += 1;
						s.SaveOrUpdate(found);
					}
				}
			}

			public static void RemoveAll(ISession s, AccountabilityNode node, DateTime now) {
				var id = node.Id;
				var all = s.QueryOver<DeepAccountability>().Where(x => (x.ParentId == id || x.ChildId == id) && x.DeleteTime == null).List().ToList();

				foreach (var a in all) {
					a.DeleteTime = now;
					s.Update(a);
				}
			}
		}
	}
}
