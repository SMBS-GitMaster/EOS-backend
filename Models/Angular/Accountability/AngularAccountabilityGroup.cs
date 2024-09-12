using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Positions;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Angular.Accountability {

	public class AngularAccountabilityGroup : BaseAngular {

		public AngularAccountabilityGroup() { }

		public AngularAccountabilityGroup(long id) : base(id) { }

		public AngularAccountabilityGroup(long groupId, long nodeId, string positionName, IEnumerable<SimpleRole> roles, bool editable) : base(groupId) {
			Position = new AngularPosition(groupId, positionName);
			if (roles != null) {
				var rg = new AngularRoleGroup(new Attach(AttachType.Node, nodeId), roles.NotNull(x => x.Select(y => new AngularRole(y)).ToList()));
				RoleGroups = new List<AngularRoleGroup>() { rg };
			}
			Editable = editable;
		}

		public AngularAccountabilityGroup(long groupId, string positionName, List<AngularRoleGroup> groups, bool editable) : base(groupId) {
			Position = new AngularPosition(groupId, positionName);
			RoleGroups = groups;
			Editable = editable;
		}

		public AngularPosition Position { get; set; }

		public bool? Editable { get; set; }

		public IEnumerable<AngularRoleGroup> RoleGroups { get; set; }

	}
}