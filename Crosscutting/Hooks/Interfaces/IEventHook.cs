using NHibernate;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Interfaces {
	public interface IEventHook : IHook {
		Task HandleEventTriggered(ISession s, IEventAnalyzer analyzer, IEventSettings settings);
	}
}