using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models {
	public class WhiteboardDiff : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual long ByUserId { get; set; }
		public virtual long OrgId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual string Delta { get; set; }
		public virtual string WhiteboardId { get; set; }
		public virtual string ElementId { get; set; }
		public virtual int Version { get; set; }
		public virtual bool Permanent { get; set; }
		public WhiteboardDiff() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<WhiteboardDiff> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.WhiteboardId).Index("IDX_WHITEBOARDDIFF_WHITEBOARDID");
				Map(x => x.OrgId);
				Map(x => x.ByUserId);
				Map(x => x.Delta);
				Map(x => x.ElementId);
				Map(x => x.Version);
				Map(x => x.Permanent);
			}
		}

	}
}
