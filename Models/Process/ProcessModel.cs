using FluentNHibernate.Mapping;
using System;

namespace RadialReview.Models.Process {
	public class ProcessModel {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual string Name { get; set; }
		public virtual string Description { get; set; }
		public virtual string ImageUrl { get; set; }
		public virtual long OrgId { get; set; }
		public virtual long ProcessFolderId { get; set; }
		public virtual long CreatorId { get; set; }
		public virtual long OwnerId { get; set; }

		public virtual bool? _Editable { get; set; }
		public virtual DateTime LastEdit { get; set; }

		public ProcessModel() {
			CreateTime = DateTime.UtcNow;
			LastEdit = CreateTime;
		}

		public class Map : ClassMap<ProcessModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.LastEdit);
				Map(x => x.DeleteTime);
				Map(x => x.Name);
				Map(x => x.Description);
				Map(x => x.ImageUrl);
				Map(x => x.OrgId);
				Map(x => x.ProcessFolderId);
				Map(x => x.CreatorId);
				Map(x => x.OwnerId);
			}
		}
	}
}
