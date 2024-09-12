using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.L10;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.EventAnalyzers.Searchers {
	public class SearchPageTimerSettings : BaseSearch<List<L10Recurrence.L10Recurrence_Page>> {
		public long RecurrenceId { get; set; }

		public SearchPageTimerSettings(long recurrenceId) {
			RecurrenceId = recurrenceId;
		}

		public override async Task<List<L10Recurrence.L10Recurrence_Page>> PerformSearch(IEventSettings settings) {
			return settings.Session.QueryOver<L10Recurrence.L10Recurrence_Page>()
				.Where(x => x.DeleteTime == null && x.L10RecurrenceId == RecurrenceId)
				.List().ToList();
		}

		protected override IEnumerable<string> UniqueKeys(IEventSettings settings) {
			yield return "" + RecurrenceId;
		}
	}

	public class SearchAllRecurrencePageTimerActuals : BaseSearch<List<L10Meeting.L10Meeting_Log>> {
		public long RecurrenceId { get; set; }
		public SearchAllRecurrencePageTimerActuals(long recurrenceId) {
			RecurrenceId = recurrenceId;
		}

		public override async Task<List<L10Meeting.L10Meeting_Log>> PerformSearch(IEventSettings settings) {
			L10Meeting alias = null;
			return settings.Session.QueryOver<L10Meeting.L10Meeting_Log>()
				.JoinAlias(x => x.L10Meeting, () => alias)
				.Where(x => x.DeleteTime == null && alias.L10RecurrenceId == RecurrenceId)
				.List().ToList();
		}

		protected override IEnumerable<string> UniqueKeys(IEventSettings settings) {
			yield return "" + RecurrenceId;
		}
	}
}
