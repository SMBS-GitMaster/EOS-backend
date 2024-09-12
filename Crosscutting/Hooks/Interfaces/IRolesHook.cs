using NHibernate;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {

	public class ISimpleRoleHookUpdates {
		public bool DeleteTimeChanged { get; set; }
		public bool NameChanged { get; set; }
		public bool OrderingChanged { get; set; }
		public bool NodeChanged { get; set; }

	}

	public interface ISimpleRoleHook : IHook {
		Task CreateRole(ISession s, long simpleRoleId);
		Task UpdateRole(ISession s, long simpleRoleId, ISimpleRoleHookUpdates updates);
	}
}
