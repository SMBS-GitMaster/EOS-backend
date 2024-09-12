using NHibernate;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Interfaces {
	public interface ILegalHooks : IHook {
		Task SubmitTerms(ISession s, string userId, string termId, string kind, string termsSha1, bool accept);
	}
}
