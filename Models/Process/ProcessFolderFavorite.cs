using FluentNHibernate.Mapping;
using System;

namespace RadialReview.Models.Process {
	public class ProcessFavorite {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long ForUser { get; set; }
		public virtual long ProcessId { get; set; }
		public ProcessFavorite() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<ProcessFavorite> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.ForUser);
				Map(x => x.ProcessId);
			}

		}
	} 
}