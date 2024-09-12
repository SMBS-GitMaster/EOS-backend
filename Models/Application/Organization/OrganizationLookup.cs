using FluentNHibernate.Mapping;
using System;

namespace RadialReview.Models {
  public class OrganizationLookup {
		public virtual long Id { get; set; }
		public virtual long OrgId { get; set; }

		public virtual long LastUserLogin { get; set; }
		public virtual DateTime LastUserLoginTime { get; set; }

		public virtual DateTime CreateTime { get; set; }

		public class Map : ClassMap<OrganizationLookup> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.OrgId).Index("OrgLookup_OrgId_Index");
				Map(x => x.LastUserLogin);
				Map(x => x.LastUserLoginTime);
				Map(x => x.CreateTime);
			}
		}
	}
}
