using RadialReview.Accessors;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Todo;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.EventAnalyzers.Searchers {
	public class SearchAllTodosForRecurrence : BaseSearch<List<TodoModel>> {
		public SearchAllTodosForRecurrence(long recurrenceId) {
			RecurrenceId = recurrenceId;
		}

		public long RecurrenceId { get; private set; }

		public override async Task<List<TodoModel>> PerformSearch(IEventSettings settings) {
			return L10Accessor.GetAllTodosForRecurrence(settings.Session, settings.Admin,RecurrenceId, includeClosed:true);
		}

		protected override IEnumerable<string> UniqueKeys(IEventSettings settings) {
			return new[] {
				"r_"+RecurrenceId,
			};
		}
	}
}