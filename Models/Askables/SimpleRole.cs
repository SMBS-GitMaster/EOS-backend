using FluentNHibernate.Mapping;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models.Askables {
	public class SimpleRole : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual long NodeId { get; set; }
		public virtual long OrgId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual string Name { get; set; }
		public virtual int Ordering { get; set; }

		[Obsolete("Don't use. For historical reasons.")]
		public virtual string AttachType_Deprecated { get; set; }

		public SimpleRole() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<SimpleRole> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.NodeId).Index("idx_simplerole_node");
				Map(x => x.OrgId).Index("idx_simplerole_org");
				Map(x => x.Name);
				Map(x => x.Ordering);
				Map(x => x.AttachType_Deprecated).Column("AttachType");
			}

		}

		public virtual AngularRole ToAngular() {
			return new AngularRole(this);
		}
	}
}