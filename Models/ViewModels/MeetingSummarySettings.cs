using RadialReview.Models.Meeting;
using System.Collections.Generic;

namespace RadialReview.Models.ViewModels {
	public class MeetingSummarySettings {
		public List<MeetingSummaryWhoModel> SendTo { get; set; }
		public long RecurrenceId { get; set; }

	}
}