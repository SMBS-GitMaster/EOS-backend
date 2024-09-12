using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models.Process.Execution {
	public class ProcessExecutionStep : ILongIdentifiable {
		public virtual long Id { get; set; }
		public virtual long ProcessId { get; set; }
		public virtual long StepId { get; set; }
		public virtual long OrgId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long ProcessExecutionId { get; set; }
		public virtual string Name { get; set; }
		public virtual string Details { get; set; }
		public virtual int Ordering { get; set; }
		public virtual long? ParentStepId { get; set; }
		public virtual long? ParentExectutionStepId { get; set; }
		public virtual DateTime? CompleteTime { get; set; }


		public ProcessExecutionStep() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<ProcessExecutionStep> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrgId);
				Map(x => x.StepId).Index("IDX_ProcessExecutionStep_StepId");
				Map(x => x.ProcessId).Index("IDX_ProcessExecutionStep_ProcessId");
				Map(x => x.ProcessExecutionId).Index("IDX_ProcessExecutionStep_ProcessExecutionId");
				Map(x => x.Name);
				Map(x => x.Details);
				Map(x => x.Ordering);
				Map(x => x.ParentStepId);
				Map(x => x.ParentExectutionStepId);
				Map(x => x.CompleteTime);
			}
		}
	}
}