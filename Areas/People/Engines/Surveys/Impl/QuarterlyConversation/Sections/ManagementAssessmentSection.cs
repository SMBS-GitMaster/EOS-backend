using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections {
  public class ManagementAssessmentSection : ISectionInitializer {

    public bool SelfAssessment { get; private set; }
    public TermsCollection Terms { get; set; }

    public ManagementAssessmentSection(bool selfAssessment, TermsCollection terms) {
      SelfAssessment = selfAssessment;
      Terms = terms;
    }

    public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
      yield break;
    }

    public IEnumerable<IItemInitializer> GetItemBuildersSupervisorSelfAssessment(IItemInitializerData data) {
      yield return new AssessmentItem(SurveyQuestionIdentifier.ManagementAssessment,
        "I clear assumptions and provide clear expectations",
          "Validate if my assumption and their assumptions are accurate",
          "We are aligned on Roles, "+Terms.GetTerm(TermKey.CoreValues)+", "+Terms.GetTerm(TermKey.Goals)+" and "+Terms.GetTerm(TermKey.Metrics)
      );
      yield return new AssessmentItem(SurveyQuestionIdentifier.ManagementAssessment,
        "I openly communicate",
          "You are understood and they are understood",
          "One positive emotion, one negative emotion",
          "Ask/tell ratio (80% them talking, 20% you talking)"
      );
      yield return new AssessmentItem(SurveyQuestionIdentifier.ManagementAssessment,
        "I have the right meeting cadence",
          "Even exchange of information and relationship building",
          "Reporting measurables",
          "Just the right amount of attention"
      );
      yield return new AssessmentItem(SurveyQuestionIdentifier.ManagementAssessment,
        "I am having "+Terms.GetTerm(TermKey.Quarterly1_1)+" with my direct reports",
          Terms.GetTerm(TermKey.Goals)+", Roles and "+Terms.GetTerm(TermKey.CoreValues),
                    Terms.GetTerm(TermKey.RightPersonRightSeat)+" (Core Values and UEC)"
            );
      yield return new AssessmentItem(SurveyQuestionIdentifier.ManagementAssessment,
        "I am celebrating and showing appreciation",
          "Communicate what is working and not working quickly (24 hours)",
          "Constructive feedback privately, showing appreciation publicly",
          "Being their boss is clear",
          "Exiting Process in place for team members who misalign with our "+Terms.GetTerm(TermKey.CoreValues)
      );

    }

    public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {
      //Not reviewing our manager
      if (data.SurveyContainer.GetSurveyType() == SurveyType.QuarterlyConversation && data.About.Is<SurveyUserNode>()) {
        if (!(data.About as SurveyUserNode)._Relationship[data.By.ToKey()].HasFlag(AboutType.Manager)) {
          return new List<IItemInitializer>();
        }
      }

      if (SelfAssessment) {
        throw new NotImplementedException();
      } else {

        if (data.FirstSeenByAbout()) {
          var sun = (data.About as SurveyUserNode);
          if (sun != null && sun._Relationship != null && sun._Relationship[data.By.ToKey()].HasFlag(AboutType.Self))
            return GetItemBuildersSupervisorSelfAssessment(data);
          return GetItemBuildersSupervisorSelfAssessment(data);//return GetItemBuildersSupervisorAssessment(data);
        }
        return new List<IItemInitializer>();

      }
    }

    public ISection InitializeSection(ISectionInitializerData data) {
      var sun = data.About as SurveyUserNode;
      var isBoss = true;//"Would you say your boss could say yes to...";
      if (sun != null && sun._Relationship != null && sun._Relationship[data.Survey.GetBy().ToKey()].HasFlag(AboutType.Self))
        isBoss= false;

      var help = "";
      var title = isBoss ? "Management Assessment" : "Management Personal Assessment";


      return new SurveySection(data, title, SurveySectionType.ManagementAssessment, "management-assessment") {
        Help = help
      };
    }

    public void Prelookup(IInitializerLookupData data) {
      //nothing to do
    }
  }
}
