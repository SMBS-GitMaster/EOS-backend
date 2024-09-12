using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RadialReview.Core.Models.Terms {
  public class TermsCollection : IEnumerable<Term> {

    public static readonly TermsCollection DEFAULT = GetTerms(null);

    public TermsCollection(string languageCode, List<Term> terms) {
      LanguageCode = languageCode;
      Terms = terms;
    }

    public string LanguageCode { get; private set; }
    public List<Term> Terms { get; private set; }

    private static Regex FILENAME_FILTER = new Regex("[^a-zA-Z-._ ]");

    public string GetTerm(TermKey key, TermModification modification = TermModification.None) {
      var res = Terms.FirstOrDefault(x => x.Key == key).NotNull(x => x.Value ?? x.Default) ?? "x";
      switch (modification) {
        case TermModification.None:
          return res;
        case TermModification.Filename:
          return FILENAME_FILTER.Replace(res, "_").Trim();
        default:
          return res;
      }



    }

    public string GetTermSingular(TermKey key) {
      return TermsPluralizer.Singularize(GetTerm(key));
    }
    public string GetTermPlural(TermKey key) {
      return TermsPluralizer.Pluralize(GetTerm(key));
    }

    public static TermsCollection GetTerms(TermsModel model) {

      //Cache empty result.
      if (model == null && DEFAULT != null)
        return DEFAULT;

      model = model ?? new TermsModel();
      var deflts = DefaultTerms.GetDefaultsByLanguage(model.LanguageCode);

      var terms = new List<Term>() {
        new Term(TermKey.WeeklyMeeting,model.WeeklyMeeting,deflts.WeeklyMeeting),
        new Term(TermKey.CheckIn,model.CheckIn,deflts.CheckIn),
        new Term(TermKey.Metrics,model.Metrics,deflts.Metrics),
        new Term(TermKey.Goals,model.Goals,deflts.Goals),
        new Term(TermKey.Headlines,model.Headlines,deflts.Headlines),
        new Term(TermKey.ToDos,model.ToDos,deflts.ToDos),
        new Term(TermKey.Issues,model.Issues,deflts.Issues),
        new Term(TermKey.WrapUp,model.WrapUp,deflts.WrapUp),
        new Term(TermKey.BusinessPlan,model.BusinessPlan,deflts.BusinessPlan),
        new Term(TermKey.DepartmentPlan,model.DepartmentPlan,deflts.DepartmentPlan),
        new Term(TermKey.FutureFocus,model.FutureFocus,deflts.FutureFocus),
        new Term(TermKey.ShortTermFocus,model.ShortTermFocus,deflts.ShortTermFocus),
        new Term(TermKey.LongTermIssues,model.LongTermIssues,deflts.LongTermIssues),
        new Term(TermKey.OrganizationalChart,model.OrganizationalChart,deflts.OrganizationalChart),
        new Term(TermKey.OrgChart,model.OrgChart,deflts.OrgChart),
        new Term(TermKey.CoreValues,model.CoreValues,deflts.CoreValues),
        new Term(TermKey.Focus,model.Focus,deflts.Focus),
        new Term(TermKey.BHAG,model.BHAG,deflts.BHAG),
        new Term(TermKey.MarketingStrategy,model.MarketingStrategy,deflts.MarketingStrategy),
        new Term(TermKey.Differentiators,model.Differentiators,deflts.Differentiators),
        new Term(TermKey.ProvenProcess,model.ProvenProcess,deflts.ProvenProcess),
        new Term(TermKey.Guarantee,model.Guarantee,deflts.Guarantee),
        new Term(TermKey.TargetMarket,model.TargetMarket,deflts.TargetMarket),
        new Term(TermKey.Visionary,model.Visionary,deflts.Visionary),
        new Term(TermKey.SecondInCommand,model.SecondInCommand,deflts.SecondInCommand),
        new Term(TermKey.ThreeYearVision,model.ThreeYearVision,deflts.ThreeYearVision),
        new Term(TermKey.OneYearGoals,model.OneYearGoals,deflts.OneYearGoals),
        new Term(TermKey.LeadAndManage,model.LeadAndManage,deflts.LeadAndManage),
        new Term(TermKey.QuarterlyPlanning,model.QuarterlyPlanning,deflts.QuarterlyPlanning),
        new Term(TermKey.AnnualPlanning,model.AnnualPlanning,deflts.AnnualPlanning),
        new Term(TermKey.Quarters,model.Quarters,deflts.Quarters),
        new Term(TermKey.EmpowerThroughChoice,model.EmpowerThroughChoice,deflts.EmpowerThroughChoice),
        new Term(TermKey.Understand,model.Understand,deflts.Understand),
        new Term(TermKey.Embrace,model.Embrace,deflts.Embrace),
        new Term(TermKey.Capacity,model.Capacity,deflts.Capacity),
        new Term(TermKey.ThinkOnTheBusiness,model.ThinkOnTheBusiness,deflts.ThinkOnTheBusiness),
        new Term(TermKey.Quarterly1_1,model.Quarterly1_1,deflts.Quarterly1_1),
        new Term(TermKey.RightPersonRightSeat,model.RightPersonRightSeat,deflts.RightPersonRightSeat),
        new Term(TermKey.QuarterlyGoals,model.QuarterlyGoals,deflts.QuarterlyGoals),
        new Term(TermKey.One_OneMeeting,model.One_OneMeeting,deflts.One_OneMeeting),
        new Term(TermKey.LaunchDay,model.LaunchDay,deflts.LaunchDay),
        new Term(TermKey.FutureFocusDay,model.FutureFocusDay,deflts.FutureFocusDay),
        new Term(TermKey.ShortTermFocusDay,model.ShortTermFocusDay,deflts.ShortTermFocusDay),
        new Term(TermKey.PurposeCausePassion,model.PurposeCausePassion,deflts.PurposeCausePassion),
        new Term(TermKey.Measurables,model.Measurables,deflts.Measurables),
        new Term(TermKey.Milestones ,model.Milestones,deflts.Milestones),
        new Term(TermKey.Niche ,model.Niche,deflts.Niche),
      };

      return new TermsCollection(model.LanguageCode, terms);
    }

    public Dictionary<string, string> GetTermsDictionary() {
      return Terms.ToDictionary(x => x.KeyString, x => x.Value ?? x.Default ?? "-");
    }

    public IEnumerator<Term> GetEnumerator() {
      return Terms.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return Terms.GetEnumerator();
    }
  }
}
