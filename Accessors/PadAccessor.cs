using Hangfire;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Hangfire;
using RadialReview.Hangfire.Activator;
using RadialReview.Middleware.Services.NotesProvider;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public class PadAccessor : BaseAccessor {

		[AutomaticRetry(Attempts = 0)]
		[Queue(HangfireQueues.Immediate.ETHERPAD)]
		public static async Task<string> HangfireCreatePad(string padId, string text, [ActivateParameter] INotesProvider notesProvider) {			
			await notesProvider.CreatePad(padId, text);
			return padId.ToString();
		}


		public static async Task<bool> CreatePad(string padid, string text = null) {
			Scheduler.Enqueue(() => HangfireCreatePad(padid, text, default(INotesProvider)));
			return true;
		}









	}
}
