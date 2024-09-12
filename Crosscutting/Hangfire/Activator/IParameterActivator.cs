using System.Reflection;

namespace RadialReview.Crosscutting.Hangfire.Activator {
	public interface IParameterActivator {
		bool ShouldApply(ParameterActivatorState state, ParameterInfo p);
		object Activate(ParameterActivatorState state, ParameterInfo p);
	}
}
