using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models.Process.Execution {
	public class ProcessExecution : ILongIdentifiable {
		public virtual long Id { get; set; }
		public virtual long ProcessId { get; set; }
		public virtual string Name { get; set; }
		public virtual string Description { get; set; }
		public virtual long ExecutedBy { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime LastModified { get; set; }
		public virtual DateTime? CompleteTime { get; set; }
		public virtual DateTime ProcessVersion { get; set; }
		public virtual long OrgId { get; set; }

		public virtual int TotalSteps { get; set; }
		public virtual int CompletedSteps { get; set; }

		public ProcessExecution() {
			CreateTime = DateTime.UtcNow;
			LastModified = CreateTime;
		}

		public class Map : ClassMap<ProcessExecution> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.LastModified);
				Map(x => x.ProcessId);
				Map(x => x.Name);
				Map(x => x.Description);
				Map(x => x.ExecutedBy);
				Map(x => x.ProcessVersion);
				Map(x => x.OrgId);
				Map(x => x.CompleteTime);
				Map(x => x.TotalSteps);
				Map(x => x.CompletedSteps);
			}
		}
	}
}