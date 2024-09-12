using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models.Process {
	public class ProcessStep : ILongIdentifiable {
		public virtual long Id { get; set; }
		public virtual long OrgId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long ProcessId { get; set; }
		public virtual string Name { get; set; }
		public virtual string Details { get; set; }
		public virtual int Ordering { get; set; }
		public virtual long? ParentStepId { get; set; }

		public ProcessStep() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<ProcessStep> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrgId);
				Map(x => x.ProcessId).Index("IDX_ProcessStep_ProcessId");
				Map(x => x.Name);
				Map(x => x.Details);
				Map(x => x.Ordering);
				Map(x => x.ParentStepId).Index("IDX_ProcessStep_ParentStepId");
			}

		}
	}
}