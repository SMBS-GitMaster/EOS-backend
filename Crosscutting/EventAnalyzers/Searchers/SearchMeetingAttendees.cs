﻿using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.L10;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.EventAnalyzers.Searchers {
	public class SearchAliveMeetingAttendees : BaseSearch<List<L10Meeting.L10Meeting_Attendee>> {

		public long RecurrenceId { get; set; }

		public SearchAliveMeetingAttendees(long recurrenceId) {
			RecurrenceId = recurrenceId;
		}

		public override async Task<List<L10Meeting.L10Meeting_Attendee>> PerformSearch(IEventSettings settings) {
			L10Meeting alias = null;
			return settings.Session.QueryOver<L10Meeting.L10Meeting_Attendee>()
									.JoinAlias(x => x.L10Meeting, () => alias)
									.Where(x => x.DeleteTime == null && alias.L10RecurrenceId == RecurrenceId)
									.List().ToList();
		}

		protected override IEnumerable<string> UniqueKeys(IEventSettings settings) {
			yield return "" + RecurrenceId;
		}
	}
	
}