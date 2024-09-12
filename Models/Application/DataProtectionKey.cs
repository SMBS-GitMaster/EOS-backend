using FluentNHibernate.Mapping;
using System;

namespace RadialReview.Models.Application {
	public class DataProtectionKey {

		public virtual string Id { get; set; }
		public virtual string FriendlyName { get; set; }
		public virtual string EncryptedElement { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual bool IsInvalid { get; set; }

		public DataProtectionKey() {
			Id = Guid.NewGuid() + "";
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<DataProtectionKey> {
			public Map() {
				Id(x => x.Id).GeneratedBy.Assigned();
				Map(x => x.FriendlyName);
				Map(x => x.EncryptedElement).Length(2000);
				Map(x => x.CreateTime);
				Map(x => x.IsInvalid);
			}

		}
	}
}
