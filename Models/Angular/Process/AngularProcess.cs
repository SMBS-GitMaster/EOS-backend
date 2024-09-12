using RadialReview.Accessors;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Process;
using RadialReview.Models.Process.Execution;
using RadialReview.Models.Process.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Angular.Process {


	public class AngularProcess : BaseAngular {
		public string Name { get; set; }
		public string Description { get; set; }
		public long? FolderId { get; set; }
		public bool? Favorite { get; set; }
		public DateTime? CreateTime { get; set; }
		[Obsolete("do not use")]
		public AngularProcess() { }
		public AngularProcess(ProcessVM p) : base(p.Id) {
			Name = p.Name;
			Description = p.Description;
			CreateTime = p.CreateTime;
			FolderId = p.ProcessFolderId;
			Favorite = p.Favorite;
		}

		public AngularProcess(ProcessModel a, bool? favorite) : base(a.Id) {
			Name = a.Name;
			Description = a.Description;
			FolderId = a.ProcessFolderId;
			Favorite = favorite;
		}
	}
	public class AngularProcessFolder : BaseAngular {
		public string Name { get; set; }
		public long? ParentFolderId { get; set; }

		[Obsolete("do not use")]
		public AngularProcessFolder() { }
		public AngularProcessFolder(FolderContentVM p) : base(p.Id) {
			Name = p.Name;
			ParentFolderId = p.ParentFolderId;
		}
	}

	public class AngularProcessExecution : BaseAngular {
		public long? ProcessId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public AngularUser Creator { get; set; }
		public DateTime? CreateTime { get; set; }
		public DateTime? CompleteTime { get; set; }
		public DateTime? LastModifiedTime { get; set; }
		public int? CompletedSteps { get; set; }
		public int? TotalSteps { get; set; }
		public bool? Complete { get; set; }
		public double? CompletionPercentage {
			get {
				if (TotalSteps != null && CompletedSteps != null && TotalSteps != 0) {
					return (((double)CompletedSteps) / ((double)TotalSteps) * 100.0);
				}
				return null;
			}
		}


		[Obsolete("do not use")]
		public AngularProcessExecution() { }
		public AngularProcessExecution(long id) : base(id) { }

		public AngularProcessExecution(ProcessVM pvm) : base(pvm.Execution.Id) {
			Name = pvm.Name;
			Description = pvm.Description;
			CreateTime = pvm.CreateTime;
			Creator = new AngularUser(pvm.CreatorId);
			ProcessId = pvm.Id;
			Complete = pvm.Execution.CompleteTime != null;
			CompleteTime = pvm.Execution.CompleteTime;
			TotalSteps = pvm.Execution.TotalSteps;
			LastModifiedTime = pvm.Execution.LastModified;
			CompletedSteps = pvm.Execution.CompletedSteps;
		}

		public AngularProcessExecution(ProcessExecution x) : base(x.Id) {
			Name = x.Name;
			CreateTime = x.CreateTime;
			Creator = new AngularUser(x.ExecutedBy);
			ProcessId = x.ProcessId;
			Complete = x.CompleteTime != null;
			CompleteTime = x.CompleteTime;
			TotalSteps = x.TotalSteps;
			CompletedSteps = x.CompletedSteps;
			LastModifiedTime = x.LastModified;
		}
	}


	public class AngularProcessStep : BaseAngular {

		public string Name { get; set; }
		public string Description { get; set; }
		public List<AngularProcessStep> Substeps { get; set; }
		[Obsolete("do not use")]
		public AngularProcessStep() { }
		public AngularProcessStep(ProcessStepVM x) : base(x.Id) {
			Name = x.Name;
			Description = x.Details;
			Substeps = x.Substeps.NotNull(y => y.Select(z => new AngularProcessStep(z)).ToList()) ?? new List<AngularProcessStep>();
		}
	}

	public class AngularProcessExecutionStep : BaseAngular {
		public string Name { get; set; }
		public string Description { get; set; }
		public long? StepId { get; set; }
		public long? ExecutionId { get; set; }
		public bool? Complete { get; set; }
		public DateTime? CompleteTime { get; set; }

		public List<AngularProcessExecutionStep> Substeps { get; set; }

		[Obsolete("do not use")]
		public AngularProcessExecutionStep() { }

		public AngularProcessExecutionStep(ProcessStepVM x) : base(x.Execution.StepId) {
			ExecutionId = x.Execution.ProcessId;
			StepId = x.Id;
			Name = x.Name;
			Description = x.Details;
			Complete = x.Execution.CompleteTime != null;
			CompleteTime = x.Execution.CompleteTime;
			Substeps = x.Substeps.NotNull(y => y.Select(z => new AngularProcessExecutionStep(z)).ToList()) ?? new List<AngularProcessExecutionStep>();
		}
	}


	public class AngularProcessList : BaseAngular {
		public string Description { get; set; }
		public AngularDateRange dataDateRange { get; set; }
		public List<AngularProcessAndExecution> ProcessList { get; set; }
		public AngularProcessList() : base(-3) { }
	}

	public class AngularProcessAndExecution : BaseAngular {

		public AngularProcess Process { get; set; }
		public List<AngularProcessExecution> ProcessExecutions { get; set; }
		[Obsolete("do not use")]
		public AngularProcessAndExecution() { }


		public AngularProcessAndExecution(long id) : base(id) {
		}

		public AngularProcessAndExecution(ProcessVM process, List<ProcessVM> executions) : base(process.Id) {
			Process = new AngularProcess(process);
			ProcessExecutions = executions.NotNull(y =>
				y.Select(x => new AngularProcessExecution(x)).ToList()
			);
		}
	}
}