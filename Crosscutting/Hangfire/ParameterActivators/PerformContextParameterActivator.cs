using Hangfire.Server;
using RadialReview.Crosscutting.Hangfire.Activator;
using System.Reflection;

namespace RadialReview.Crosscutting.Hangfire.ParameterActivators {
	public class PerformContextParameterActivator : IParameterActivator {


		public object Activate(ParameterActivatorState state, ParameterInfo p) {
			return state.PerformContext;
		}

		public bool ShouldApply(ParameterActivatorState state, ParameterInfo p) {
			return p.ParameterType == typeof(PerformContext);
		}
	}
}
