using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Enums;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.PermissionsListers {

	public class NameIdCreatableGroupablePermissions : NameIdCreatablePermissions {
		public long GroupId { get; set; }
		public bool Noteworthy { get; set; }
		public NameIdCreatableGroupablePermissions(long id, string name, bool canCreate, long groupId, bool noteworthy) : base(id, name, canCreate) {
			GroupId = groupId;
			Noteworthy = noteworthy;
		}
	}

	public class NameIdCreatablePermissions {
		public NameIdCreatablePermissions(long id, string name, bool canCreate) {
			Id = id;
			CanCreate = canCreate;
			Name = name;
		}
		public long Id { get; set; }
		public bool CanCreate { get; set; }
		public string Name { get; set; }
	}
	public class UserPermissionsHelper {


		public static List<NameIdCreatablePermissions> GetUsersWeCanCreateRocksFor(UserOrganizationModel caller, long forUserId, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetUsersWeCanCreateMeetingItemsFor(s, perms, forUserId, orgId);
				}
			}
		}

		public static List<NameIdCreatablePermissions> GetUsersWeCanCreateMeasurablesFor(UserOrganizationModel caller, long forUserId, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetUsersWeCanCreateMeetingItemsFor(s, perms, forUserId, orgId);
				}
			}
		}

		/// <summary>
		/// Returns a list of userOrgIds that we can create goals for
		/// </summary>
		/// <returns></returns>
		public static List<NameIdCreatablePermissions> GetUsersWeCanCreateMeetingItemsFor(ISession s, PermissionsUtility perms, long forUserId, long orgId) {
			/*
			 * ALSO UPDATE : PermissionsUtility.CreateRocksForUser
			 */
			perms.Self(forUserId);
			perms.ViewOrganization(orgId);



			var ctx = PermissionsUtility.MultiUserContext.FromOrganization(s, perms, orgId);



			var allowedIds = perms.MultiUserCanAdminMeetingWithUsers(ctx).Where(x => x.Value).Select(x => x.Key).ToList();
			var visibleIds = allowedIds.ToList();
			visibleIds.AddRange(ctx.SubordinateAndSelfIds.Value);
			var names = ctx.SelectedUsers.Value.ToDefaultDictionary(x => x.Id, x => x.GetName());

			return visibleIds
					.Distinct()
					.Select(x => new NameIdCreatablePermissions(x, names[x], allowedIds.Any(y => y == x)))
					.OrderBy(x => x.Name)
					.ToList();
		}

		public class NodesAndRoot {
			public List<NameIdCreatableGroupablePermissions> Nodes { get; set; }
			public long RootNodeId { get; set; }
		}


		public static NodesAndRoot GetNodesWeCanCreateUsersUnder(UserOrganizationModel caller, long forUserId, long orgId, bool includeImages) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetNodesWeCanCreateUsersUnder(s, perms, forUserId, orgId, includeImages);
				}
			}
		}

		public static NodesAndRoot GetNodesWeCanCreateUsersUnder(ISession s, PermissionsUtility perms, long forUserId, long orgId, bool includeImages) {
			perms.Self(forUserId);
			perms.ViewOrganization(orgId);
			var org = s.Get<OrganizationModel>(orgId);
			var chart = s.Get<AccountabilityChart>(org.AccountabilityChartId);

			var strictHierarchy = org.StrictHierarchy;
			var permittedToEditHierarchy = perms.IsPermitted(x => x.EditHierarchy(org.AccountabilityChartId));

			var allNodes = DeepAccessor.Nodes.GetNodesForOrganization(s, perms, orgId);
			var allowed = new DefaultDictionary<long, bool>(x => false);
			if (permittedToEditHierarchy) {
				if (strictHierarchy) {
					//only add children
					var meAndChildNodeIds = DeepAccessor.Nodes.GetChildrenAndSelfGivenUserId(s, perms, forUserId);
					foreach (var n in meAndChildNodeIds) {
						allowed[n] = true;
					}
				} else {
					//add all nodes
					allNodes.ForEach(x => { allowed[x.Id] = true; });
				}
			}

			var outputNodes= allNodes.Select(x => {
				var position = x.GetPositionName();
				var names = x.GetUserNames(GivenNameFormat.FirstAndLast, "");
				var images = "";
				if (includeImages) {
					images = string.Join("", x.GetUserImages(3).Select(y => y.ToHtml()));
				}
				var name = "";
				bool noteworthy = true;
				if (string.IsNullOrWhiteSpace(position) && string.IsNullOrWhiteSpace(names)) {
					name = "<div class='images'>" + images + "</div><div class='text'><div class='users'>No users</div> <div class='function'>No function</div></div>";
					noteworthy = false;
				} else if (string.IsNullOrWhiteSpace(position)) {
					name = "<div class='images'>" + images + "</div><div class='text'><div class='users'>" + names + "</div> <div class='function'>No function</div></div>";
				} else if (string.IsNullOrWhiteSpace(names)) {
					name = "<div class='images'>" + images + "</div><div class='text'><div class='users'>No users</div> <div class='function'>" + position + "</div></div>";
				} else {
					name = "<div class='images'>" + images + "</div><div class='text'><div class='users'>" + names + "</div> <div class='function'>" + position + "</div></div>";
				}
				if (x.Id == chart.RootId) {
					name = "<div class='images'>" + images + "</div><div class='text'><div class='users root'>Root</div> <div class='function '>(Top of the Organizational Chart)</div></div>";
				}
				return new NameIdCreatableGroupablePermissions(x.Id, name, allowed[x.Id], x.ParentNodeId ?? AccountabilityAccessor.MANAGERNODE_NO_MANAGER, noteworthy);
			}).ToList();


			return new NodesAndRoot() {
				Nodes = outputNodes,
				RootNodeId = chart.RootId,
			};






		}
	}
}
