using FluentNHibernate.Mapping;
using System;


namespace RadialReview.Models.Process {
	public class ProcessFolder {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrgId { get; set; }
		public virtual long? ParentFolderId { get; set; }
		public virtual String Name { get; set; }
		public virtual bool Root { get; set; }
		public virtual long CreatorId { get; set; }
		public virtual string ImageUrl { get; set; }

		public ProcessFolder() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<ProcessFolder> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrgId);
				Map(x => x.ParentFolderId);
				Map(x => x.Name);
				Map(x => x.Root);
				Map(x => x.ImageUrl);
				Map(x => x.CreatorId);

			}

		}
	}
}