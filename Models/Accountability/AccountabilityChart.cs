using FluentNHibernate.Mapping;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RadialReview.Models.Accountability {

	public class AccountabilityChart : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual string Name { get; set; }
		public virtual long RootId { get; set; }


		public AccountabilityChart() {
			CreateTime = DateTime.UtcNow;
		}

		class Map : ClassMap<AccountabilityChart> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.Name);
				Map(x => x.RootId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrganizationId);
			}
		}

	}


	public class RoleGroupRole_Deprecated {
		public RoleGroupRole_Deprecated(RoleModel_Deprecated role, long? ordering) {
			Role = role;
			Ordering = ordering;
		}
		public RoleModel_Deprecated Role { get; set; }
		public long? Ordering { get; set; }

	}

	public class RoleGroup {
		[Obsolete("For historical reasons only", true)]
		public virtual long AttachId { get; set; }

		[Obsolete("For historical reasons only", true)]
		public virtual AttachType AttachType { get; set; }

		[Obsolete("For historical reasons only", true)]
		public virtual String AttachName { get; set; }


		public virtual List<SimpleRole> Roles { get; set; }
	}




	public class RoleGroup_Deprecated {
		public virtual long AttachId { get; set; }
		public virtual AttachType AttachType { get; set; }
		public virtual List<RoleGroupRole_Deprecated> Roles { get; set; }
		public virtual String AttachName { get; set; }

		public RoleGroup_Deprecated(List<RoleGroupRole_Deprecated> roles, long attachId, AttachType attachType, string attachName) {
			AttachId = attachId;
			AttachType = attachType;
			Roles = roles;
			AttachName = attachName;
		}

		public virtual Attach GetAttach() {
			return new Attach {
				Id = AttachId,
				Name = AttachName,
				Type = AttachType,
			};
		}

		public AngularRoleGroup ToAngular() {
			var roles = Roles.NotNull(y => y.Select(x => AngularRole.CreateDeprecated(x)).ToList());
			return new AngularRoleGroup(new Attach(AttachType, AttachId, AttachName), roles);
		}
	}

	[DebuggerDisplay("ARG: {PositionName}, {GetRolesCountAsString()} roles")]
	public class AccountabilityRolesGroup : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual string PositionName { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual List<RoleGroup> _Roles { get; set; }
		public virtual long AccountabilityChartId { get; set; }
		public virtual bool? _Editable { get; set; }

		public virtual string GetRolesCountAsString() {
			return _Roles.NotNull(x => ""+x.Count()) ?? "?";
		}

		public AccountabilityRolesGroup() {
			CreateTime = DateTime.UtcNow;
		}
		class Map : ClassMap<AccountabilityRolesGroup> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrganizationId);
				Map(x => x.AccountabilityChartId);
				Map(x => x.PositionName);

				Map(x => x.DepricatedPositionId).Column("PositionId");
				References(x => x.DepricatedPosition).Column("PositionId").LazyLoad().ReadOnly();
			}
		}
		[Obsolete("Deprecated. use PositionName instead")]
		public virtual long? DepricatedPositionId { get; set; }
		[Obsolete("Deprecated. use PositionName instead")]
		public virtual Deprecated.OrganizationPositionModel DepricatedPosition { get; set; }
	}



	public class AccountabilityNodeRoleMap_Deprecated : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long RoleId { get; set; }
		public virtual RoleModel_Deprecated Role { get; set; }
		public virtual long? PositionId { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual long AccountabilityGroupId { get; set; }
		public virtual long AccountabilityChartId { get; set; }
		public AccountabilityNodeRoleMap_Deprecated() {
			CreateTime = DateTime.UtcNow;
		}
		class Map : ClassMap<AccountabilityNodeRoleMap_Deprecated> {
			public Map() {
				Table("AccountabilityNodeRoleMap");
				Id(x => x.Id);
				Map(x => x.RoleId).Column("RoleId");
				References(x => x.Role).Column("RoleId").Not.LazyLoad().ReadOnly();
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrganizationId);
				Map(x => x.PositionId);
				Map(x => x.AccountabilityGroupId);
				Map(x => x.AccountabilityChartId);
			}
		}
	}
}