using RadialReview.Areas.People.Engines.Surveys.Interfaces;

namespace RadialReview.Areas.People.Engines.Surveys.Strategies.Events {
	public class SurveyReconstructionEventsNoOp : ISurveyReconstructorEvents {
        public void OnBegin(IOuterLookup outerLookup) {}
    }
}