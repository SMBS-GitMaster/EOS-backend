using NHibernate;
using RadialReview.Accessors;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Models.Angular.Process;
using RadialReview.Models.Process;
using RadialReview.Models.Process.Execution;
using RadialReview.Models.Process.ViewModels;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Realtime.Process {
	public class RealTime_Process : IProcessExecutionHook, IProcessExecutionStepHook {
		public bool AbsorbErrors() {
			return true;
		}

		public bool CanRunRemotely() {
			return false;
		}


		public async Task ProcessExecutionConclusionForced(ISession s, long forcedByUserId, ProcessExecution processExecution) {
			await using (var rt = RealTimeUtility.Create()) {
				rt.UpdateUsers(forcedByUserId).Update(new AngularProcessExecution(processExecution.Id) { Hide = true });
			}
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task ProcessExecutionCompleted(ISession s, bool complete, long completedById, ProcessExecution processExecution) {
			await using (var rt = RealTimeUtility.Create()) {
				rt.UpdateUsers(completedById).Update(new AngularProcessExecution(processExecution.Id) {
					Hide = complete
				});
			}

		}

		public async Task ProcessExecutionStarted(ISession s, long startedById, ProcessExecution processExecution) {

			var p = s.Get<ProcessModel>(processExecution.ProcessId);
			var favorite = ProcessAccessor.IsFavorite_Unsafe(s, startedById, processExecution.ProcessId);
			var processAndExecution = new AngularProcessAndExecution(ProcessVM.CreateFromProcess(p, favorite), new List<ProcessVM>() {
				ProcessVM.CreateFromProcessExecution(p,processExecution,null,false,null)
			});

			await using (var rt = RealTimeUtility.Create()) {
				rt.UpdateUsers(startedById).Update(new ListDataVM(startedById) {
					ProcessList = AngularList.CreateFrom(AngularListType.Add, processAndExecution)
				});
			}
		}

		public async Task CompleteStep(ISession s, bool complete, long completedById, ProcessExecution processExecution, ProcessExecutionStep stepExecution) {
			await using (var rt = RealTimeUtility.Create()) {
				rt.UpdateUsers(completedById).Update(new AngularProcessExecution(processExecution.Id) {
					CompletedSteps = processExecution.CompletedSteps
				});
			}
		}
	}
}