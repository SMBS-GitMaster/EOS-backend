using RadialReview.Crosscutting.Hangfire.Activator;
using System.Reflection;

namespace RadialReview.Crosscutting.Hangfire.ParameterActivators {
	public class ServiceParameterActivator : IParameterActivator {
		public object Activate(ParameterActivatorState state, ParameterInfo p) {
			return state.ServiceScope.ServiceProvider.GetService(p.ParameterType);
		}

		public bool ShouldApply(ParameterActivatorState state, ParameterInfo p) {
			return state.ServiceScope.ServiceProvider.GetService(p.ParameterType) != null;
		}
	}
}
