using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RadialReview.Models.Angular.Accountability {
	[SwaggerName(Name = "Seat")]
	public class AngularAccountabilityNode : AngularTreeNode<AngularAccountabilityNode> {
		public AngularAccountabilityNode() { }
		public AngularAccountabilityNode(long id) : base(id) { }

		public AngularAccountabilityNode(AccountabilityNode node, bool collapse = false, bool? editable = null) : this(
			node.GetAccountabilityRolesGroupId(),
			node.Id,
			node.Ordering,
			TryGetPositionFromNode(node),
			TryGetChildren(node, editable ?? node._Editable ?? true),
			TryGetUsersFromNode(node),
			TryGetRolesFromNode(node),
			collapse,
			editable ?? node._Editable ?? true
		) { }

		public AngularAccountabilityNode(long groupId, long nodeId, int nodeOrder, string position, IEnumerable<AngularAccountabilityNode> children, IEnumerable<UserOrganizationModel> users, IEnumerable<SimpleRole> roles, bool collapse = false, bool editable = true) : base(nodeId) {
			var isEditable = editable;
			Users = users.NotNull(x => x.Select(y => AngularUser.CreateUser(y)));
			Editable = isEditable;
			Group = new AngularAccountabilityGroup(groupId, nodeId, position, roles, isEditable);
			collapsed = collapse;
			__children = children;
			Name = Users.NotNull(y => string.Join(", ", y.Select(x => x.Name)));
			order = nodeOrder;

		}

		public string Name { get; set; }
		public AngularAccountabilityGroup Group { get; set; }
		public bool? Highlight;


		public string GetUserNames(int count = int.MaxValue, string excessAppend = "+{0} more") {
			excessAppend = excessAppend ?? "";
			if (HasUsers()) {
				var users = Users.Select(x => x.Name).Take(count).ToList();
				var res = string.Join(", ", users);
				if (users.Count > count) {
					res = res + string.Format(excessAppend, users.Count - count);
				}
				return res;
			}
			return "";
		}
		public IEnumerable<string> GetRoles(int count = int.MaxValue, string excessAppend = "+{0} more") {
			excessAppend = excessAppend ?? "";
			if (HasRoles()) {
				var roles = Group.RoleGroups.SelectMany(x => x.Roles.Select(y => y.Name)).Take(count).ToList();
				if (roles.Count > count) {
					var excess = string.Format(excessAppend, roles.Count - count);
					if (!string.IsNullOrEmpty(excess))
						roles.Add(excess);
				}
				return roles;
			}
			return new List<string>();
		}

		public IEnumerable<AngularUser> Users { get; set; }

		[IgnoreDataMember]
		public bool? _hasParent;
		[IgnoreDataMember]
		public bool _hidePdf;

		public bool HasUsers() {
			if (Users == null)
				return false;
			return Users.Any();
		}
		public bool HasRoles() {
			if (Group == null || Group.RoleGroups == null || !Group.RoleGroups.Any())
				return false;
			return Group.RoleGroups.SelectMany(x => x.Roles.Select(y => y.Name)).Any();
		}
		public bool HasUser(long userId) {
			return (Users != null) && (Users.Any(x => x.Id == userId));
		}
		public bool HasChildren() {
			return __children != null && __children.Any();
		}
		public void ExpandAll() {
			collapsed = false;
			foreach (var c in GetDirectChildren()) {
				c.ExpandAll();
			}
		}
		public void ExpandLevels(int levels) {
			if (levels <= 0)
				return;
			collapsed = false;
			foreach (var child in GetDirectChildren()) {
				child.ExpandLevels(levels - 1);

			}
		}
		public void CollapseAll() {
			collapsed = true;
			foreach (var c in GetDirectChildren()) {
				c.CollapseAll();
			}
		}
		public bool ShowNode(long nodeId) {
			if (nodeId == Id) {
				collapsed = false;
				return true;
			}
			foreach (var c in GetDirectChildren()) {
				if (c.ShowNode(nodeId)) {
					collapsed = false;
					return true;
				}
			}
			return false;
		}

		public void ForEach(Action<AngularAccountabilityNode> action) {
			action(this);
			foreach (var child in GetDirectChildren()) {
				child.ForEach(action);
			}
		}
		public void ForEachVisible(Action<AngularAccountabilityNode> action) {
			action(this);
			if (!collapsed) {
				foreach (var child in GetDirectChildren()) {
					child.ForEachVisible(action);
				}
			}
		}


		public bool ShowUser(long userId) {
			var anyTrue = false;
			if (Users.Any(x => x.Id == userId)) {
				collapsed = false;
				anyTrue = true;
			}
			foreach (var c in GetDirectChildren()) {
				if (c.ShowUser(userId)) {
					collapsed = false;
					anyTrue = true;
				}
			}
			return anyTrue;
		}
		private static IEnumerable<UserOrganizationModel> TryGetUsersFromNode(AccountabilityNode node) {
			if (node.AreUsersPopulated()) {
				return node.GetUsers(null);
			}
			return null;
		}
		private static IEnumerable<SimpleRole> TryGetRolesFromNode(AccountabilityNode node) {
			return node.GetRoles();
		}
		private static string TryGetPositionFromNode(AccountabilityNode node) {
			return node.GetPositionName();
		}
		private static IEnumerable<AngularAccountabilityNode> TryGetChildren(AccountabilityNode node, bool isEditable) {
			return node._Children.NotNull(x => x.Select(y => new AngularAccountabilityNode(y, editable: y._Editable ?? isEditable)).ToList());
		}
	}
}
