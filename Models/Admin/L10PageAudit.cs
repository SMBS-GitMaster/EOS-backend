using FluentNHibernate.Mapping;
using RadialReview.Models.L10;
using System;

namespace RadialReview.Models.Admin {
	public class L10PageAudit {
		public virtual long Id { get; set; }
		public virtual long Time { get; set; }
		public virtual long? RevertTime { get; set; }
		public virtual long OrgId { get; set; }
		public virtual long RecurId { get; set; }
		public virtual long PageId { get; set; }

		public virtual string OldName { get; set; }
		public virtual string NewName { get; set; }

		public virtual int OldOrder { get; set; }
		public virtual int NewOrder { get; set; }

		public virtual L10Recurrence.L10PageType ExpectedPageType { get; set; }
		public virtual decimal ExpectedDuration { get; set; }

		public virtual bool Reverted { get; set; }

		public class Map : ClassMap<L10PageAudit> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.Time);
				Map(x => x.PageId);
				Map(x => x.OldName);
				Map(x => x.NewName);
				Map(x => x.OldOrder);
				Map(x => x.NewOrder);

				Map(x => x.OrgId);
				Map(x => x.RecurId);
				Map(x => x.Reverted);
				Map(x => x.RevertTime);

				Map(x => x.ExpectedPageType);
				Map(x => x.ExpectedDuration);
			}

		}
	}
}
