using System.Threading.Tasks;
using NHibernate;
using RadialReview.Models;

namespace RadialReview.Utilities.Hooks {
	public class TangentHookUpdates {
		public bool TangentShowed { get; set; }
	}

	public interface ITangentHook : IHook {

		//THIS METHOD IS NEVER CALLED...
		Task ShowTangent(ISession s, UserOrganizationModel caller, long meetingId);
	}
}
