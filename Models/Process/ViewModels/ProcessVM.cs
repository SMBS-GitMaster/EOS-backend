using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RadialReview.Models.Process.Execution;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Process.ViewModels {

	public enum ProcessAccess {
		View,
		Edit,
		Execute,
	}

	public class ProcessVM {
		public long Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string ImageUrl { get; set; }
		public long ProcessFolderId { get; set; }
		public long CreatorId { get; set; }
		public long OwnerId { get; set; }
		public bool Editable { get { return ProcessAccess.Edit == AccessKind; } }

		public bool Favorite { get; set; }

		[JsonConverter(typeof(StringEnumConverter))]
		public ProcessAccess AccessKind { get; set; }
		public DateTime CreateTime { get; set; }
		public DateTime LastEditedTime { get; set; }
		public List<ProcessStepVM> Substeps { get; set; }
		public ProcessExecutionVM Execution { get; set; }

		private ProcessVM(ProcessModel process, bool favorite) {
			Id = process.Id;
			Name = process.Name;
			Description = process.Description;
			ImageUrl = process.ImageUrl;
			ProcessFolderId = process.ProcessFolderId;
			CreatorId = process.CreatorId;
			OwnerId = process.OwnerId;
			AccessKind = process._Editable == true ? ProcessAccess.Edit : ProcessAccess.View;
			CreateTime = process.CreateTime;
			Substeps = new List<ProcessStepVM>();
			LastEditedTime = process.LastEdit;
			Favorite = favorite;
		}

		public static ProcessVM CreateFromProcess(ProcessModel process, bool isFavorite) {
			return new ProcessVM(process, isFavorite);
		}

		public static ProcessVM CreateFromProcessExecution(ProcessModel process, ProcessExecution exec, List<ProcessExecutionStep> steps, bool isFavorite, Action<long, HashSet<string>> modifications) {
			return new ProcessVM(process, isFavorite) {
				Id = exec.ProcessId,
				Name = exec.Name,
				Description = exec.Description,
				CreateTime = exec.CreateTime,
				Execution = new ProcessExecutionVM() {
					Id = exec.Id,
					CompleteTime = exec.CompleteTime,
					TotalSteps = exec.TotalSteps,
					CompletedSteps = exec.CompletedSteps,
					LastModified = exec.LastModified
				},
				AccessKind = ProcessAccess.Execute,
				Substeps = steps.NotNull(y => y.OrderBy(x => x.Ordering).Where(x => x.ParentExectutionStepId == null).Select(x => BuildSubsteps(x.Id, steps, modifications)).ToList()),

			};
		}
		public static ProcessStepVM BuildSubsteps(long id, List<ProcessExecutionStep> allSteps, Action<long, HashSet<string>> modifications) {

			var found = allSteps.Single(x => x.Id == id);
			var output = new ProcessStepVM(found);
			if (modifications != null) {
				var hs = new HashSet<string>();
				modifications(found.StepId, hs);
				output.Modifiers.AddRange(hs);
			}

			var children = new List<ProcessStepVM>();
			foreach (var a in allSteps.Where(x => x.ParentExectutionStepId == id).OrderBy(x => x.Ordering)) {
				children.Add(BuildSubsteps(a.Id, allSteps, modifications));
			}
			output.Substeps = children;
			return output;
		}
		public class ProcessExecutionVM {
			public long Id { get; set; }
			public DateTime LastModified { get; set; }
			public DateTime? CompleteTime { get; set; }
			public int CompletedSteps { get; set; }
			public int TotalSteps { get; set; }
		}
	}

	public class ProcessStepVM {
		public long Id { get; set; }

		public string Name { get; set; }
		public string Details { get; set; }
		public long ProcessId { get; set; }
		public long? ParentStepId { get; set; }
		public List<string> Modifiers { get; set; }
		public List<ProcessStepVM> Substeps { get; set; }
		public ProcessStepExecutionVM Execution { get; set; }


		public ProcessStepVM(ProcessStep step) {
			Id = step.Id;
			Name = step.Name;
			Details = step.Details;
			ProcessId = step.ProcessId;
			ParentStepId = step.ParentStepId;
			Substeps = new List<ProcessStepVM>();
			Modifiers = new List<string>();

		}

		public ProcessStepVM(ProcessExecutionStep executionStep) {
			Id = executionStep.StepId;
			Name = executionStep.Name;
			Details = executionStep.Details;
			ProcessId = executionStep.ProcessId;
			ParentStepId = executionStep.ParentExectutionStepId;
			Substeps = new List<ProcessStepVM>();
			Modifiers = new List<string>();

			Execution = new ProcessStepExecutionVM() {
				CompleteTime = executionStep.CompleteTime,
				ParentStepId = executionStep.ParentExectutionStepId,
				StepId = executionStep.Id,
				ProcessId = executionStep.ProcessExecutionId
			};
		}
		public class ProcessStepExecutionVM {
			public long ProcessId { get; set; }
			public long StepId { get; set; }
			public long? ParentStepId { get; set; }
			public DateTime? CompleteTime { get; set; }

		}
	}
}
