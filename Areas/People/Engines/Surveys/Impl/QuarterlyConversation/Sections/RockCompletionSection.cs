using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System.Collections.Generic;
using RadialReview.Models.Interfaces;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models.Enums;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections {
	public class RockCompletionSection : ISectionInitializer {
        public static string NUMBER_LAST_QUARTER = "NLQ";
        public static string NUMBER_COMPLETE_LAST_QUARTER = "NCLQ";

        public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
            return new IItemInitializer[] { };
        }

        public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {
			if (data.FirstSeenByAbout()) {
				var aboutSUN = (data.About as SurveyUserNode);
				if (aboutSUN != null && aboutSUN._Relationship[data.By.ToKey()].HasFlag(AboutType.Subordinate)) {
					yield return new InputItemIntializer("# of Goals completed last Quarter", SurveyQuestionIdentifier.RockCompletion, SurveyItemType.Number, new KV[] { new KV("QN", NUMBER_COMPLETE_LAST_QUARTER) });
					yield return new InputItemIntializer("# of Goals last Quarter", SurveyQuestionIdentifier.RockCompletion, SurveyItemType.Number, new KV[] { new KV("QN", NUMBER_LAST_QUARTER) });
				}
			}
            yield break;
        }

        public ISection InitializeSection(ISectionInitializerData data) {
            return new SurveySection(data, "Goal Completion", SurveySectionType.RockCompletion, "mk-rockcompletion");
        }

        public void Prelookup(IInitializerLookupData data) {
            //noop
        }
    }
}