using RadialReview.Models.Angular.Base;
using RadialReview.Models.L10;

namespace RadialReview.Models.Angular.Scorecard {
	public class AngularMeetingMeasurable : BaseAngular {
		public AngularMeetingMeasurable(L10Meeting.L10Meeting_Measurable measurable) : base(measurable.Id) {
			Measurable = new AngularMeasurable(measurable.Measurable);
			Measurable.RecurrenceMeasurableId = measurable.Id;

		}
		public AngularMeasurable Measurable { get; set; }


	}
}