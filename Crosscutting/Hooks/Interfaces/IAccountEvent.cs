using NHibernate;
using RadialReview.Models.Events;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public interface IAccountEvent : IHook{
		Task CreateEvent(ISession s, AccountEvent evt);
	}
}
