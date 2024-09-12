using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using RadialReview.Models.Enums;
using RadialReview.Models.Accountability;
using RadialReview.Models.Reviews;
using System.Threading.Tasks;

namespace RadialReview.Accessors {

	public class PosDur {
		public long UserId { get; set; }
		public long PosId { get; set; }
		public string PosName { get; set; }
		public DateTime _StartTime { get; set; }
		public DateTime? _DeleteTime { get; set; }
	}
	public class TeamDur {
		public long UserId { get; set; }
		public long TeamId { get; set; }
		public string TeamName { get; set; }
		public DateTime _StartTime { get; set; }
		public DateTime? _DeleteTime { get; set; }
	}
	public class RoleAccessor {


		public class Deprecated {
			#region GetRoleLinks_Unsafe
			public static void UndeleteRole(UserOrganizationModel caller, long id) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						PermissionsUtility.Create(s, caller).EditSimpleRole(id);
						var r = s.Get<SimpleRole>(id);
						r.DeleteTime = null;
						s.Update(r);
						tx.Commit();
						s.Flush();
					}
				}
			}

			public static async Task EditRole(UserOrganizationModel caller, long id, string role, DateTime? deleteTime = null) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						PermissionsUtility.Create(s, caller).EditRole_Deprecated(id);
						var r = s.Get<RoleModel_Deprecated>(id);

						var updateSent = false;
						r.Role = role;
						if (r.DeleteTime != deleteTime) {
							r.DeleteTime = deleteTime;
							updateSent = true;
						}
						s.Update(r);

						tx.Commit();
						s.Flush();
					}
				}
			}

			public class RoleLinksQuery_Deprecated {

				public RoleLinksQuery_Deprecated(IEnumerable<RoleModel_Deprecated> roles, IEnumerable<RoleLink_Deprecated> roleLinks, IEnumerable<TeamDurationModel> teams, IEnumerable<PositionDurationModel> positions, IEnumerable<AccountabilityNodeUserMap> userNodes, DateRange range = null) {
					Teams = teams;
					Positions = positions;
					RoleLinks = roleLinks;
					DateRange = range;
					Roles = roles;
					UserNodes = userNodes;
				}

				public IEnumerable<TeamDurationModel> Teams { get; set; }
				public IEnumerable<PositionDurationModel> Positions { get; set; }
				public IEnumerable<AccountabilityNodeUserMap> UserNodes { get; set; }
				public IEnumerable<RoleLink_Deprecated> RoleLinks { get; set; }
				public IEnumerable<RoleModel_Deprecated> Roles { get; set; }
				public DateRange DateRange { get; set; }

				private IEnumerable<RoleDetails_Deprecated> ConstructRoleDetails(RoleLink_Deprecated link) {
					var role = Roles.Where(x => x.Id == link.RoleId).Where(DateRange.Filter<RoleModel_Deprecated>().Compile()).SingleOrDefault();
					if (role != null)
						yield return new RoleDetails_Deprecated(role, link);
					yield break;
				}

				public IEnumerable<RoleDetails_Deprecated> GetRoleDetailsForUser(long forUserId) {

					var range = DateRange;
					var teams = Teams.Where(x => x.UserId == forUserId).Where(range.Filter<TeamDurationModel>().Compile());
					var pos = Positions.Where(x => x.UserId == forUserId).Where(range.Filter<PositionDurationModel>().Compile());
					var seats = UserNodes.Where(x => x.UserId == forUserId).Where(range.Filter<AccountabilityNodeUserMap>().Compile());

					var rangeLinks = RoleLinks.Where(range.Filter<RoleLink_Deprecated>().Compile());

					var userRoleLinks = rangeLinks.Where(x => x.AttachType == AttachType.User && x.AttachId == forUserId).SelectMany(ConstructRoleDetails);
					var teamRoleLinks = rangeLinks.Where(x => x.AttachType == AttachType.Team && (teams.Any(y => y.TeamId == x.AttachId))).SelectMany(ConstructRoleDetails);
					var posRoleLinks = rangeLinks.Where(x => x.AttachType == AttachType.Position && (pos.Any(y => y.DepricatedPosition.Id == x.AttachId))).SelectMany(ConstructRoleDetails);
					var seatRoleLinks = rangeLinks.Where(x => x.AttachType == AttachType.Node && (seats.Any(y => y.AccountabilityNodeId == x.AttachId))).SelectMany(ConstructRoleDetails);

					return userRoleLinks.Union(teamRoleLinks).Union(posRoleLinks).Union(seatRoleLinks);
				}

				public class RoleDetails_Deprecated {
					public RoleDetails_Deprecated(RoleModel_Deprecated role, RoleLink_Deprecated roleLink) {
						Role = role;
						RoleLink = roleLink;
					}
					public RoleModel_Deprecated Role { get; set; }
					public RoleLink_Deprecated RoleLink { get; set; }
					public Attach RoleComesFrom { get { return RoleLink.GetAttach(); } }
				}


			}
			#region GetRoles
			[Obsolete("broken", true)]
			public List<RoleModel_Deprecated> GetRoles(UserOrganizationModel caller, long userId, DateRange range = null) {
				throw new NotImplementedException();
			}
			//Update Both GetRoles Methods
			[Obsolete("broken", true)]
			public static List<RoleModel_Deprecated> GetRoles(AbstractQuery queryProvider, PermissionsUtility perms, long forUserId, DateRange range = null) {
				throw new NotImplementedException();
			}

			[Obsolete("deprecated")]
			public static List<RoleModel_Deprecated> GetRolesForReviewee(ISession s, AbstractQuery queryProvider, PermissionsUtility perms, Reviewee revieweeUser, DateRange range = null) {
				var forUserId = revieweeUser.RGMId;
				if (revieweeUser.ACNodeId == null)
					return GetRoles(queryProvider, perms, revieweeUser.RGMId, range);
				else {
					return GetRolesForAcNode(s, queryProvider, perms, revieweeUser.ACNodeId.Value, range)
								.OrderBy(x => x.Ordering)
								.Select(x => x.Role)
								.ToList();
				}
			}



			//Update Both GetRoles Methods
			[Obsolete("broken", true)]
			public static List<RoleModel_Deprecated> GetRoles(ISession s, PermissionsUtility perms, long forUserId, DateRange range = null) {
				throw new NotImplementedException();


			}

			#endregion


			[Untested("Hooks")]
			[Obsolete("replace me", true)]
			public async Task EditRoles(UserOrganizationModel caller, long userId, List<RoleModel_Deprecated> roles, bool updateOutstanding) {
				throw new NotImplementedException();
			}

			[Obsolete("broken", true)]
			public static int CountRoles(ISession s, PermissionsUtility perms, long userId) {
				throw new NotImplementedException();

			}

			[Obsolete("Deprecated")]
			public static List<RoleGroupRole_Deprecated> GetRolesForAcNode(ISession s, AbstractQuery queryProvider, PermissionsUtility perms, long acNodeId, DateRange range = null) {

				var node = queryProvider.Get<AccountabilityNode>(acNodeId);
				perms.ViewOrganization(node.OrganizationId);


				var teamDur = new List<TeamDurationModel>();
				var allRoleLinks = new List<RoleLink_Deprecated>();
				foreach (var nodeUserId in node.GetUsers(s).SelectId()) {
					perms.ViewUserOrganization(nodeUserId, false);
					teamDur = queryProvider.Where<TeamDurationModel>(x =>
						x.OrganizationId == node.OrganizationId && x.UserId == nodeUserId
					).FilterRange(range).ToList();

					var userRoleLinks = queryProvider.Where<RoleLink_Deprecated>(x => x.OrganizationId == node.OrganizationId && x.AttachType == AttachType.User && x.AttachId == nodeUserId).FilterRange(range).ToList();
					allRoleLinks.AddRange(userRoleLinks);
				}

				var posDur = new List<PositionDurationModel>();
				if (node.AccountabilityRolesGroup.DepricatedPositionId != null) {
					posDur = queryProvider.Where<PositionDurationModel>(x =>
							x.OrganizationId == node.OrganizationId && x.DepricatedPosition.Id == node.AccountabilityRolesGroup.DepricatedPositionId
						).FilterRange(range).ToList();
				}

				if (teamDur.Any()) {
					var teamsRoleLinks = queryProvider.WhereRestrictionOn<RoleLink_Deprecated>(x => x.OrganizationId == node.OrganizationId && x.AttachType == AttachType.Team, x => x.AttachId, teamDur.Select(x => x.TeamId).Cast<object>()).FilterRange(range).ToList();
					allRoleLinks.AddRange(teamsRoleLinks);
				}
				if (posDur.Any()) {
					var positionsRoleLinks = queryProvider.WhereRestrictionOn<RoleLink_Deprecated>(x => x.OrganizationId == node.OrganizationId && x.AttachType == AttachType.Position, x => x.AttachId, posDur.Select(x => x.DepricatedPosition.Id).Cast<object>()).FilterRange(range).ToList();
					allRoleLinks.AddRange(positionsRoleLinks);
				}


				var allRoles = queryProvider.WhereRestrictionOn<RoleModel_Deprecated>(x => x.OrganizationId == node.OrganizationId, x => x.Id, allRoleLinks.Select(x => x.RoleId).Cast<object>())
					.FilterRange(range).Distinct(x => x.Id).ToList();


				var roleLU = allRoles.ToDictionary(x => x.Id, x => x);

				var pd = posDur.Select(x => new PosDur() {
					PosName = x.DepricatedPosition.GetName(),
					PosId = x.DepricatedPosition.Id,
					UserId = x.UserId
				}).ToList();

				var td = teamDur.Select(x => new TeamDur() {
					TeamName = x.Team.GetName(),
					TeamId = x.TeamId,
					UserId = x.UserId
				}).ToList();


				return ConstructRolesForNode(node.Id, node.GetUsers(s).SelectId().ToList(), node.AccountabilityRolesGroup.DepricatedPositionId, roleLU, allRoleLinks, pd, td, null).SelectMany(x => x.Roles).ToList();


			}

			[Obsolete("broken", true)]
			public static List<RoleGroup_Deprecated> ConstructRolesForNode(long nodeId, List<long> userIds, long? positionId, Dictionary<long, RoleModel_Deprecated> rolesLU, List<RoleLink_Deprecated> links, List<PosDur> pd, List<TeamDur> td, Dictionary<long, string> userNameLookup) {
				throw new NotImplementedException();
			}
		}
		#endregion


		public class RADeprecated {
			public static List<RoleGroup_Deprecated> ConstructRolesForNode(long? userId, long? positionId, Dictionary<long, RoleModel_Deprecated> rolesLU,List<RoleLink_Deprecated> links, List<PosDur> pd, List<TeamDur> td) {

				var relaventPD = pd.Where(x => x.PosId == positionId).ToList();


				var relaventTD = td.Where(x => x.UserId == userId).ToList();

				var relaventGroups = new List<RoleGroup_Deprecated>();
				if (userId != null) {
					var userRoleLinks = links.Where(x => x.AttachType == AttachType.User && x.AttachId == userId);
					var userRoles = userRoleLinks.Select(x => new RoleGroupRole_Deprecated(rolesLU.GetOrDefault(x.RoleId, null), x.Ordering)).Where(x => x != null && x.Role != null).ToList();
					if (userRoles.Any())
						relaventGroups.Add(new RoleGroup_Deprecated(userRoles, userId.Value, AttachType.User, "User"));

				}
				{
					var roles = new List<RoleModel_Deprecated>();

					var posGroup = new DefaultDictionary<long, RoleGroup_Deprecated>(x => new RoleGroup_Deprecated(new List<RoleGroupRole_Deprecated>(), x, AttachType.Position, "Function"));

					if (positionId != null) {
						var baseGroup = posGroup[positionId.Value];
						var myPosRolesLinks = links.Where(x => x.AttachType == AttachType.Position && x.AttachId == positionId);
						var posRoles = myPosRolesLinks.Select(x => new RoleGroupRole_Deprecated(rolesLU.GetOrDefault(x.RoleId, null), x.Ordering)).Where(x => x != null && x.Role != null).ToList();
						posGroup[positionId.Value].Roles.AddRange(posRoles);
					}

					var posRolesLinks = links.Where(x => x.AttachType == AttachType.Position && relaventPD.Any(y => y.PosId == x.AttachId) && x.AttachId != positionId);
					foreach (var pos in posRolesLinks.GroupBy(x => x.AttachId)) {
						var posRoles = pos.Select(x => new RoleGroupRole_Deprecated(rolesLU.GetOrDefault(x.RoleId, null), x.Ordering)).Where(x => x != null && x.Role != null).ToList();
						posGroup[pos.Key].Roles.AddRange(posRoles);
					}

					foreach (var group in posGroup) {
						relaventGroups.Add(group.Value);
					}
				}
				{
					var teamRolesLinks = links.Where(x => x.AttachType == AttachType.Team && relaventTD.Any(y => y.TeamId == x.AttachId));

					foreach (var team in teamRolesLinks.GroupBy(x => x.AttachId)) {
						var teamRoles = team.Select(x => new RoleGroupRole_Deprecated(rolesLU.GetOrDefault(x.RoleId, null), x.Ordering)).Where(x => x != null && x.Role != null).ToList();
						if (teamRoles.Any()) {
							var teamName = td.FirstOrDefault(y => y.TeamId == team.Key).NotNull(y => y.TeamName) ?? "Team";
							relaventGroups.Add(new RoleGroup_Deprecated(teamRoles, team.Key, AttachType.Team, teamName));
						}
					}
				}



				return relaventGroups.ToList();
			}

		}
	}
}
