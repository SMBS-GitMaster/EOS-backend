using NHibernate;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {

	public class IPositionHookUpdates {
		public bool NameChanged { get; set; }
		public bool WasDeleted { get; set; }
	}
}
