using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Models.Interfaces;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models.Enums;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections {
  public class LeadershipAssessmentSection : ISectionInitializer {
    public bool SelfAssessment { get; private set; }
    public TermsCollection Terms { get; set; }

    public LeadershipAssessmentSection(bool selfAssessment, TermsCollection terms) {
      SelfAssessment = selfAssessment;
      Terms = terms;
    }

    public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
      yield break;
    }

    public IEnumerable<IItemInitializer> GetItemBuildersSupervisorSelfAssessment(IItemInitializerData data) {

      yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
        "I am giving clear direction",
          "I am Creating The Space",
          "I provide an inspiring vision",
          Terms.GetTerm(TermKey.BusinessPlan)
      );
      yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
        "I am providing access to",
          "Resources",
          "Continuing education",
          "Technical Support",
          "Team members and outsourced contractors",
          "My availability"
      );
      yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
        "I am letting go of the vine",
          "Our team is "+Terms.GetTerm(TermKey.EmpowerThroughChoice),
          "Our team members "+Terms.GetTerm(TermKey.Understand)+", "+Terms.GetTerm(TermKey.Embrace)+" & have "+ Terms.GetTerm(TermKey.Capacity) +" for their roles"
            //"Our team members Get, Want, and have the Capacity to perform their roles well"
            );
      yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
        "I act with the best interest in mind",
          Terms.GetTerm(TermKey.BusinessPlan),
          "My actions executed in my integrity",
          "My choices",
          "My example",
          "Company goals first"

      );
      yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
        "I am "+Terms.GetTerm(TermKey.ThinkOnTheBusiness),
          "Get clear on priorities",
          "Protecting who I say I am",
          "Scheduled regularly",
          "Tech free (no distractions)"
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
          return GetItemBuildersSupervisorSelfAssessment(data); //GetItemBuildersSupervisorAssessment(data);
        }
        return new List<IItemInitializer>();
      }
    }

    public ISection InitializeSection(ISectionInitializerData data) {
      var sun = data.About as SurveyUserNode;
      var isBoss = true;
      if (sun != null && sun._Relationship != null && sun._Relationship[data.Survey.GetBy().ToKey()].HasFlag(AboutType.Self)) {
        isBoss = false;
      }
      var help = "";
      var title = isBoss ? "Leadership Assessment" : "Leadership Personal Assessment";


      return new SurveySection(data, title, SurveySectionType.LeadershipAssessment, "leadership-assessment") {
        Help = help
      };
    }

    public void Prelookup(IInitializerLookupData data) {
      //nothing to do
    }
  }

  public class AssessmentItem : IItemInitializer {
    public string Help { get; private set; }
    public string Name { get; private set; }
    public SurveyQuestionIdentifier QuestionIdentifier { get; private set; }

    public string Bullets(params string[] items) {
      return string.Join("\n", items.Select(x => "• " + x));
    }

    public AssessmentItem(SurveyQuestionIdentifier questionIdentifier, string name, params string[] bullets) {
      Name = name;
      Help = Bullets(bullets);
      QuestionIdentifier = questionIdentifier;
    }

    public IItem InitializeItem(IItemInitializerData data) {
      return new SurveyItem(data, Name, null, Name, Help);
    }

    public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx) {
      var options = new Dictionary<string, string>() {
          { "yes","Yes" },
          { "no","No" },
      };
      return ctx.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateRadio(ctx, QuestionIdentifier, options), Name);
    }

    public bool HasResponse(IResponseInitializerCtx data) {
      return true;
    }

    public IResponse InitializeResponse(IResponseInitializerCtx data, IItemFormat format) {
      return new SurveyResponse(data, format);
    }

    public void Prelookup(IInitializerLookupData data) {
      //nothing to do
    }
  }
}
