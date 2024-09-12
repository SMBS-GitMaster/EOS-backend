using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.UserModels;
using RadialReview.Models.UserTemplate;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public partial class AccountabilityAccessor : BaseAccessor {
		public class AADeprecated {


			[Obsolete("Use DeepAccessor.GetDirectReports", true)]
			public static List<AccountabilityNode> GetDirectReports(UserOrganizationModel caller, long forNodeId) {
				throw new NotImplementedException();
			}


			[Todo]
			protected static AngularAccountabilityNode Dive_Deprecated(UserOrganizationModel caller, long nodeId, List<AccountabilityNode> nodes,
				List<AccountabilityRolesGroup> groups, Dictionary<long, RoleModel_Deprecated> rolesLU, List<RoleLink_Deprecated> links, List<PosDur> positions,
				List<TeamDur> teams, List<AngularAccountabilityNode> parents, HashSet<long> allManagingUserIds, Dictionary<long, string> usersNameLookup, long? selectedNode = null,
				bool? editableBelow = null, bool expandAll = false, bool editSelf = false) {

				

				var me = nodes.FirstOrDefault(x => x.Id == nodeId);

				var children = nodes.Where(x => x.ParentNodeId == nodeId).ToList();


				//Calculate Permissions
				var isEditable = false;
				
				if (caller.Id == me.DeprecatedUserId && !children.Any() && !caller.ManagingOrganization) {
					isEditable = caller.Organization.Settings.EmployeesCanEditSelf;
				} else if (editableBelow == true) {
					isEditable = true;

				}

				var isMe = false;
				if (editableBelow != null && me.DeprecatedUserId != null && caller.Id == me.DeprecatedUserId) {

					editableBelow = true;
					isMe = true;
				}


				if (isMe && editSelf) {
					isEditable = true;
				}

				var group = groups.First(x => x.Id == me.GetAccountabilityRolesGroupId());

				var roleGroups = RoleAccessor.RADeprecated.ConstructRolesForNode(me.DeprecatedUserId, me.AccountabilityRolesGroup.DepricatedPositionId, rolesLU, links, positions, teams); 

				var aRoleGroups = roleGroups.Select(x => x.ToAngular()).ToList();

				var aaGroup = new AngularAccountabilityGroup(group.Id, group.PositionName, aRoleGroups, editable: isEditable);

				var aan = new AngularAccountabilityNode() {
					Id = nodeId,
					Users = AngularUser.CreateUser(me.DeprecatedUser).AsList(),
					Group = aaGroup,
					collapsed = !expandAll,
					Editable = isEditable,
					Me = isMe,
					order = me.Ordering,
				};

				if (isEditable && me.DeprecatedUserId.HasValue) {
					allManagingUserIds.Add(me.DeprecatedUserId.Value);
				}

				var parentsCopy = parents.ToList();

				parentsCopy.Add(aan);

				if (selectedNode == nodeId) {
					foreach (var p in parentsCopy) {
						p.collapsed = false;
					}
				}


				aan.SetChildren(children.Select(x => Dive_Deprecated( caller, x.Id, nodes, groups, rolesLU, links, positions, teams, parentsCopy, allManagingUserIds, usersNameLookup, selectedNode, editableBelow, expandAll)).ToList());

				return aan;
			}

			public static AngularAccountabilityChart GetTree_Deprecated(ISession s, PermissionsUtility perms, long chartId, long? centerUserId = null, long? centerNodeId = null, DateRange range = null, bool expandAll = false) {
				perms.ViewHierarchy(chartId);

				var editSelf = perms.GetCaller().Organization.Settings.EmployeesCanEditSelf;
				if (perms.GetCaller().IsManager()) {
					editSelf = editSelf || perms.GetCaller().Organization.Settings.ManagersCanEditSelf;
				}

				var chart = s.Get<AccountabilityChart>(chartId);

				var orgId = chart.OrganizationId;

				var nodesQ = s.QueryOver<AccountabilityNode>().Where(x => x.AccountabilityChartId == chartId).Where(range.Filter<AccountabilityNode>()).Future();
				PositionModel posAlias = null;
				var groupsQ = s.QueryOver<AccountabilityRolesGroup>()
					.Where(x => x.AccountabilityChartId == chartId)
					.Where(range.Filter<AccountabilityRolesGroup>())
					.Future();

				var userTemplatesQ = s.QueryOver<UserTemplate_Deprecated>().Where(x => x.OrganizationId == orgId).Where(range.Filter<UserTemplate_Deprecated>()).Future();
				var rolesQ = s.QueryOver<RoleModel_Deprecated>().Where(x => x.OrganizationId == orgId).Where(range.Filter<RoleModel_Deprecated>()).Future();
				var roleLinksQ = s.QueryOver<RoleLink_Deprecated>().Where(x => x.OrganizationId == orgId).Where(range.Filter<RoleLink_Deprecated>()).Future();

				var teamsQ = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).Where(range.Filter<OrganizationTeamModel>()).Select(x => x.Id, x => x.Name).Future<object[]>();
				var positionsQ = s.QueryOver<Deprecated.OrganizationPositionModel>().Where(x => x.Organization.Id == orgId).Where(range.Filter<Deprecated.OrganizationPositionModel>()).Select(x => x.Id, x => x.CustomName).Future<object[]>();

				var teamDursQ = s.QueryOver<TeamDurationModel>().Where(x => x.OrganizationId == orgId).Where(range.Filter<TeamDurationModel>()).Select(x => x.TeamId, x => x.UserId).Future<object[]>();
				var posDursQ = s.QueryOver<PositionDurationModel>().Where(x => x.OrganizationId == orgId).Where(range.Filter<PositionDurationModel>()).Select(x => x.DepricatedPosition.Id, x => x.UserId).Future<object[]>();

				var userLookupsQ = s.QueryOver<UserLookup>().Where(x => x.OrganizationId == orgId).Where(range.Filter<UserLookup>()).Future();
				List<AccountabilityNodeUserMap> accNodeUsers = GetAccNodeUses_Deprecated(s, chartId, range);
				List<UserOrganizationModel> usersF = GetUsersInRange_Deprecated(s, range, orgId);

				var nodes = nodesQ.ToList();
				var groups = groupsQ.ToList();
				var userTemplates = userTemplatesQ.ToList();
				var roles = rolesQ.ToList();
				var roleLinks = roleLinksQ.ToList();
				var teams = teamsQ.ToList();
				var positions = positionsQ.ToList();
				var teamDurs = teamDursQ.ToList();
				var userLookups = userLookupsQ.ToList();
				var posDurs = posDursQ.ToList();

				foreach (var node in nodes) {
					node.SetUsers(accNodeUsers);
				}

				var nameLookup = new Dictionary<long, string>();
				foreach (var anu in accNodeUsers.Select(x => x.User)) {
					nameLookup[anu.Id] = anu.GetName();
				}


				var teamName = teams.ToDictionary(x => (long)x[0], x => (string)x[1]);
				var posName = positions.ToDictionary(x => (long)x[0], x => (string)x[1]);

				var pd = posDurs.Where(x => x[0] != null).Select(x => new PosDur {
					PosId = (long)x[0],
					PosName = posName.GetOrDefault((long)x[0], null),
					UserId = (long)x[1]
				}).ToList();
				var td = teamDurs.Select(x => new TeamDur { TeamId = (long)x[0], TeamName = posName.GetOrDefault((long)x[0], null), UserId = (long)x[1] }).ToList();

				var centerNode = chart.RootId;
				if (centerNodeId != null) {
					var cn = nodes.FirstOrDefault(x => x.Id == centerNodeId);
					if (cn != null)
						centerNode = cn.Id;
				} else if (centerUserId != null) {
					var cn = nodes.FirstOrDefault(x => x.GetUsers(s).SelectId().Contains(centerUserId.Value));
					if (cn != null)
						centerNode = cn.Id;
				}

				var editAC = perms.IsPermitted(x => x.EditHierarchy(chart.Id));
				var editAll = perms.IsPermitted(x => x.Or(y => y.ManagingOrganization(orgId), y => y.EditHierarchy(chart.Id)));

				var allManaging = new HashSet<long>();

				var root = Dive_Deprecated( perms.GetCaller(), chart.RootId, nodes.ToList(), groups.ToList(), roles.ToDictionary(x => x.Id, x => x), roleLinks.ToList(), pd, td, new List<AngularAccountabilityNode>(), allManaging, nameLookup, centerNode, editableBelow: editAll, expandAll: expandAll, editSelf: editSelf);

				var allUsers = usersF.ToList().Select(x =>
					AngularUser.CreateUser(x, managing: editAll || allManaging.Contains(x.Id) || (perms.GetCaller().IsManager() && perms.GetCaller().Id == x.Id))
				).ToList();

				var c = new AngularAccountabilityChart(chartId) {
					Root = root,
					CenterNode = centerNode,
					AllUsers = allUsers,
					CanReorganize = editAC,
				};

				c.Root.Name = chart.Name;

				return c;
			}

			private static List<AccountabilityNodeUserMap> GetAccNodeUses_Deprecated(ISession s, long chartId, DateRange range) {
				AccountabilityNode nodeAlias = null;
				var accNodeUsersQ = s.QueryOver<AccountabilityNodeUserMap>()
										.JoinAlias(x => x.AccountabilityNode, () => nodeAlias)
										.Where(x => x.ChartId == chartId)
										.Where(range.Filter<AccountabilityNodeUserMap>());
				if (range == null) {
					accNodeUsersQ = accNodeUsersQ.Where(_ => nodeAlias.DeleteTime == null);
				} else {
					accNodeUsersQ = accNodeUsersQ.Where(_ => nodeAlias.CreateTime <= range.EndTime && (nodeAlias.DeleteTime == null || nodeAlias.DeleteTime >= range.StartTime));
				}
				var accNodeUsers = accNodeUsersQ.Fetch(x => x.User).Eager.List().ToList();
				return accNodeUsers;
			}

			private static List<UserOrganizationModel> GetUsersInRange_Deprecated(ISession s, DateRange range, long orgId) {
				return s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId && !x.IsClient).Where(range.Filter<UserOrganizationModel>()).List().ToList();
			}
		}
	}
}
