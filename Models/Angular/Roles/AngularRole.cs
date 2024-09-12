using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;

namespace RadialReview.Models.Angular.Roles {
	public class AngularRole : BaseAngular {

		public AngularRole() { }

		public AngularRole(long id) : base(id) { }



		public AngularRole(SimpleRole x) : base(x.Id) {
			Name = x.Name;
			CreateTime = x.CreateTime;
			Ordering = x.Ordering;
		}

		[Obsolete("Deprecated")]
		public static AngularRole CreateDeprecated(RoleModel_Deprecated role, long? ordering = null) {
			return new AngularRole(role.Id) {
				Name = role.Role,
				Ordering = (int?)((ordering).NotNull(x => x.Value % int.MaxValue)),
				CreateTime = role.CreateTime,
			};
		}
		[Obsolete("Deprecated")]
		public static AngularRole CreateDeprecated(RoleGroupRole_Deprecated role) {
			return CreateDeprecated(role.Role, role.Ordering);
		}

		[Obsolete("use SimpleRole instead", true)]
		public AngularRole(RoleGroupRole_Deprecated role) : this(role.Role, role.Ordering) {
		}

		[Obsolete("use SimpleRole instead", true)]
		public AngularRole(RoleModel_Deprecated role, long? ordering = null) : base(role.Id) {
			Name = role.Role;
			CreateTime = role.CreateTime;
			Ordering = (int?)(ordering.NotNull(x => x.Value % int.MaxValue));
		}
		public string Name { get; set; }
		public int? Ordering { get; set; }
		public DateTime? CreateTime { get; set; }

	}

	public class AngularRoleGroup : BaseAngular {

		public AngularRoleGroup() { }

		public AttachType? AttachType { get; set; }
		public long? AttachId { get; set; }
		public String AttachName { get; set; }

		public bool? Editable { get; set; }

		public AngularRoleGroup(long id) : base(id) { }

		public static long GetId(Attach attach) {
			return attach.Id * (long)RadialReview.Models.Enums.AttachType.MAX + (long)attach.Type;
		}

		public IEnumerable<AngularRole> Roles { get; set; }


		public AngularRoleGroup(Attach attach, IEnumerable<AngularRole> roles, bool? editable = null) : base(GetId(attach)) {
			AttachType = attach.Type;
			AttachId = attach.Id;
			AttachName = attach.Name;
			Roles = roles;
			Editable = editable;
		}

		public static AngularRoleGroup CreateForNode(long nodeId, IEnumerable<AngularRole> roles, bool? editable = null) {
			return new AngularRoleGroup(new Attach(Enums.AttachType.Node, nodeId), roles, editable);
		}
	}

}
