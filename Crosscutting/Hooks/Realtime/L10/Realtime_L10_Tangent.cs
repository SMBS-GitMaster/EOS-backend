using System.Threading.Tasks;
using NHibernate;
using RadialReview.Models;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;

namespace RadialReview.Crosscutting.Hooks.Realtime.L10 {
	public class Realtime_L10_Tangent : ITangentHook {
		public bool AbsorbErrors() {
			return false;
		}

		public bool CanRunRemotely() {
			return true;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task ShowTangent(ISession s, UserOrganizationModel caller, long recurrenceId) {
      await using (var rt = RealTimeUtility.Create())
      {
        rt.UpdateRecurrences(recurrenceId).Call("showTangentAlert");
      }
    }
	}
}