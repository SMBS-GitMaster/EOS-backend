using NHibernate;
using RadialReview.Accessors;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Interfaces {
	public interface IMeetingModeHook : IHook {
		Task StartL10Mode(ISession s, long recurrenceId, Mode mode);

		/// <summary>
		/// Notice: I0f the mode is deleted or renamed, mode may be null.
		/// </summary>
		/// <param name="s"></param>
		/// <param name="recurrenceId"></param>
		/// <param name="mode"></param>
		/// <param name="modeName"></param>
		/// <returns></returns>
		Task RevertL10Mode(ISession s, long recurrenceId, Mode mode, string modeName);
	}
}
