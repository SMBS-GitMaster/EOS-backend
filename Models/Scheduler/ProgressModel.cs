using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models.Scheduler {
	public class ProgressModel : IStringIdentifiable, IHistorical {
		public virtual String Id { get; set; }
		public virtual long OrgId { get; set; }
		public virtual long CreatedBy { get; set; }
		public virtual string TaskName { get; set; }
		public virtual double TotalCount { get; set; }
		public virtual double CompletedCount { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime? CompleteTime { get; set; }
		public virtual string Data { get; set; }
		public virtual int Errors { get; set; }

		public ProgressModel() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<ProgressModel> {
			public Map() {
				Id(x => x.Id).GeneratedBy.Assigned().Length(36);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.TotalCount);
				Map(x => x.CompletedCount);
				Map(x => x.CompleteTime);
				Map(x => x.CreatedBy);
				Map(x => x.TaskName);
				Map(x => x.OrgId);
				Map(x => x.Data);
				Map(x => x.Errors);
			}

		}

	}
}