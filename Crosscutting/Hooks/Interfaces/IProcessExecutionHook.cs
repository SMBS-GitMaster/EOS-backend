using NHibernate;
using RadialReview.Models.Process.Execution;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Interfaces {
	public interface IProcessExecutionStepHook : IHook {
		Task CompleteStep(ISession s, bool complete, long completedById, ProcessExecution processExecution, ProcessExecutionStep stepExecution);
	}
	public interface IProcessExecutionHook : IHook {
		Task ProcessExecutionStarted(ISession s, long startedById, ProcessExecution processExecution);
		Task ProcessExecutionCompleted(ISession s, bool complete, long completedById,ProcessExecution processExecution);
		Task ProcessExecutionConclusionForced(ISession s, long forcedByUserId, ProcessExecution processExecution);
	}
}
