using RadialReview.Crosscutting.Hangfire.Activator;
using RadialReview.Crosscutting.Hangfire.Jobs;
using System.Reflection;

namespace RadialReview.Crosscutting.Hangfire.ParameterActivators {
	public class JobInfoParameterActivator : IParameterActivator {
		public object Activate(ParameterActivatorState state, ParameterInfo p) {
			return state.JobInfo;
		}
		public bool ShouldApply(ParameterActivatorState state, ParameterInfo p) {
			return p.ParameterType == typeof(JobInfo);
		}
	}

	public class RecurringJobDataParameterActivator : IParameterActivator {

		public object Activate(ParameterActivatorState state, ParameterInfo p) {
			return state.JobInfo.RecurringJobInfo;
		}

		public bool ShouldApply(ParameterActivatorState state, ParameterInfo p) {
			return p.ParameterType == typeof(RecurringJobInfo);
		}
	}
}
