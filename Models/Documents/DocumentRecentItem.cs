using FluentNHibernate.Mapping;
using RadialReview.Models.Documents.Enums;
using System;

namespace RadialReview.Models.Documents {



	public class DocumentItemLookupCache {
		public virtual long Id { get; set; }
		public virtual long ForUser { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DocumentItemType ItemType { get; set; }
		public virtual long ItemId { get; set; }
		public virtual DocumentItemLookupCacheKind CacheKind { get; set; }

		public DocumentItemLookupCache() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<DocumentItemLookupCache> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.ForUser);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.ItemType);
				Map(x => x.ItemId);
				Map(x => x.CacheKind);
			}

		}
	}
}