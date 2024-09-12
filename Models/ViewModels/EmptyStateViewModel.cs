using static RadialReview.Models.L10.L10Recurrence;

namespace RadialReview.Models.ViewModels {
	public class EmptyStateViewModel {
		public string Header { get; set; }
		public string ImageSource { get; set; }
		public string ImageAlternate { get; set; }
		public string ButtonText { get; set; }
		public string BodyMessage { get; set; }
		public L10PageType PageType { get; set; }
		public bool Hidden { get; set; }
	}
}