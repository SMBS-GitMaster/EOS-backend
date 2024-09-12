using System;
using static RadialReview.Models.L10.L10Recurrence;

namespace RadialReview.Core.Models.Terms {
  public enum TermKey {
    WeeklyMeeting,
    CheckIn,
    Metrics,
    Goals,
    Headlines,
    ToDos,
    Issues,
    WrapUp,
    BusinessPlan,
    DepartmentPlan,
    FutureFocus,
    ShortTermFocus,
    LongTermIssues,
    OrganizationalChart,
    OrgChart,
    CoreValues,
    Focus,
    BHAG,
    MarketingStrategy,
    Differentiators,
    ProvenProcess,
    Guarantee,
    TargetMarket,
    Visionary,
    SecondInCommand,
    ThreeYearVision,
    OneYearGoals,
    //BloomGrowth,
    LeadAndManage,
    QuarterlyPlanning,
    AnnualPlanning,
    Quarters,
    EmpowerThroughChoice,
    //UnderstandEmbraceCapacity,
    ThinkOnTheBusiness,
    Quarterly1_1,
    RightPersonRightSeat,
    QuarterlyGoals,
    One_OneMeeting,
    LaunchDay,
    FutureFocusDay,
    ShortTermFocusDay,
    //GoalsRolesCoreValues,
    PurposeCausePassion,
    Measurables,
    Milestones,
    Understand,
    Embrace,
    Capacity,
    Niche,
  }
  public class TermKeyToTermsModel {
    public static void SetValue(TermsModel terms, TermKey key, string value) {
      switch (key) {
        case TermKey.WeeklyMeeting:
          terms.WeeklyMeeting = value;
          break;
        case TermKey.CheckIn:
          terms.CheckIn = value;
          break;
        case TermKey.Metrics:
          terms.Metrics = value;
          break;
        case TermKey.Goals:
          terms.Goals = value;
          break;
        case TermKey.Headlines:
          terms.Headlines = value;
          break;
        case TermKey.ToDos:
          terms.ToDos = value;
          break;
        case TermKey.Issues:
          terms.Issues = value;
          break;
        case TermKey.WrapUp:
          terms.WrapUp = value;
          break;
        case TermKey.BusinessPlan:
          terms.BusinessPlan = value;
          break;
        case TermKey.DepartmentPlan:
          terms.DepartmentPlan = value;
          break;
        case TermKey.FutureFocus:
          terms.FutureFocus = value;
          break;
        case TermKey.ShortTermFocus:
          terms.ShortTermFocus = value;
          break;
        case TermKey.LongTermIssues:
          terms.LongTermIssues = value;
          break;
        case TermKey.OrganizationalChart:
          terms.OrganizationalChart = value;
          break;
        case TermKey.OrgChart:
          terms.OrgChart = value;
          break;
        case TermKey.CoreValues:
          terms.CoreValues = value;
          break;
        case TermKey.Focus:
          terms.Focus = value;
          break;
        case TermKey.BHAG:
          terms.BHAG = value;
          break;
        case TermKey.MarketingStrategy:
          terms.MarketingStrategy = value;
          break;
        case TermKey.Differentiators:
          terms.Differentiators = value;
          break;
        case TermKey.ProvenProcess:
          terms.ProvenProcess = value;
          break;
        case TermKey.Guarantee:
          terms.Guarantee = value;
          break;
        case TermKey.TargetMarket:
          terms.TargetMarket = value;
          break;
        case TermKey.Visionary:
          terms.Visionary = value;
          break;
        case TermKey.SecondInCommand:
          terms.SecondInCommand = value;
          break;
        case TermKey.ThreeYearVision:
          terms.ThreeYearVision = value;
          break;
        case TermKey.OneYearGoals:
          terms.OneYearGoals = value;
          break;
        case TermKey.LeadAndManage:
          terms.LeadAndManage = value;
          break;
        case TermKey.QuarterlyPlanning:
          terms.QuarterlyPlanning = value;
          break;
        case TermKey.AnnualPlanning:
          terms.AnnualPlanning = value;
          break;
        case TermKey.Quarters:
          terms.Quarters = value;
          break;
        case TermKey.EmpowerThroughChoice:
          terms.EmpowerThroughChoice = value;
          break;
        case TermKey.Understand:
          terms.Understand = value;
          break;
        case TermKey.Embrace:
          terms.Embrace = value;
          break;
        case TermKey.Capacity:
          terms.Capacity = value;
          break;
        case TermKey.ThinkOnTheBusiness:
          terms.ThinkOnTheBusiness = value;
          break;
        case TermKey.Quarterly1_1:
          terms.Quarterly1_1 = value;
          break;
        case TermKey.RightPersonRightSeat:
          terms.RightPersonRightSeat = value;
          break;
        case TermKey.QuarterlyGoals:
          terms.QuarterlyGoals = value;
          break;
        case TermKey.One_OneMeeting:
          terms.One_OneMeeting = value;
          break;
        case TermKey.LaunchDay:
          terms.LaunchDay = value;
          break;
        case TermKey.FutureFocusDay:
          terms.FutureFocusDay = value;
          break;
        case TermKey.ShortTermFocusDay:
          terms.ShortTermFocusDay = value;
          break;
        case TermKey.PurposeCausePassion:
          terms.PurposeCausePassion = value;
          break;
        case TermKey.Measurables:
          terms.Measurables = value;
          break;
        case TermKey.Milestones:
          terms.Milestones = value;
          break;
        case TermKey.Niche:
          terms.Niche = value;
          break;

        default:
          throw new ArgumentOutOfRangeException("Unhandled key:" + key);
      }


    }


    public static string GetValue(TermsModel terms, TermKey key) {
      switch (key) {
        case TermKey.WeeklyMeeting:
          return terms.WeeklyMeeting;
        case TermKey.CheckIn:
          return terms.CheckIn;
        case TermKey.Metrics:
          return terms.Metrics;
        case TermKey.Goals:
          return terms.Goals;
        case TermKey.Headlines:
          return terms.Headlines;
        case TermKey.ToDos:
          return terms.ToDos;
        case TermKey.Issues:
          return terms.Issues;
        case TermKey.WrapUp:
          return terms.WrapUp;
        case TermKey.BusinessPlan:
          return terms.BusinessPlan;
        case TermKey.DepartmentPlan:
          return terms.DepartmentPlan;
        case TermKey.FutureFocus:
          return terms.FutureFocus;
        case TermKey.ShortTermFocus:
          return terms.ShortTermFocus;
        case TermKey.LongTermIssues:
          return terms.LongTermIssues;
        case TermKey.OrganizationalChart:
          return terms.OrganizationalChart;
        case TermKey.OrgChart:
          return terms.OrgChart;
        case TermKey.CoreValues:
          return terms.CoreValues;
        case TermKey.Focus:
          return terms.Focus;
        case TermKey.BHAG:
          return terms.BHAG;
        case TermKey.MarketingStrategy:
          return terms.MarketingStrategy;
        case TermKey.Differentiators:
          return terms.Differentiators;
        case TermKey.ProvenProcess:
          return terms.ProvenProcess;
        case TermKey.Guarantee:
          return terms.Guarantee;
        case TermKey.TargetMarket:
          return terms.TargetMarket;
        case TermKey.Visionary:
          return terms.Visionary;
        case TermKey.SecondInCommand:
          return terms.SecondInCommand;
        case TermKey.ThreeYearVision:
          return terms.ThreeYearVision;
        case TermKey.OneYearGoals:
          return terms.OneYearGoals;
        case TermKey.LeadAndManage:
          return terms.LeadAndManage;
        case TermKey.QuarterlyPlanning:
          return terms.QuarterlyPlanning;
        case TermKey.AnnualPlanning:
          return terms.AnnualPlanning;
        case TermKey.Quarters:
          return terms.Quarters;
        case TermKey.EmpowerThroughChoice:
          return terms.EmpowerThroughChoice;
        case TermKey.Understand:
          return terms.Understand;
        case TermKey.Embrace:
          return terms.Embrace;
        case TermKey.Capacity:
          return terms.Capacity;
        case TermKey.ThinkOnTheBusiness:
          return terms.ThinkOnTheBusiness;
        case TermKey.Quarterly1_1:
          return terms.Quarterly1_1;
        case TermKey.RightPersonRightSeat:
          return terms.RightPersonRightSeat;
        case TermKey.QuarterlyGoals:
          return terms.QuarterlyGoals;
        case TermKey.One_OneMeeting:
          return terms.One_OneMeeting;
        case TermKey.LaunchDay:
          return terms.LaunchDay;
        case TermKey.FutureFocusDay:
          return terms.FutureFocusDay;
        case TermKey.ShortTermFocusDay:
          return terms.ShortTermFocusDay;
        case TermKey.PurposeCausePassion:
          return terms.PurposeCausePassion;
        case TermKey.Measurables:
          return terms.Measurables;
        case TermKey.Milestones:
          return terms.Milestones;
        case TermKey.Niche:
          return terms.Niche;
        default:
          throw new ArgumentOutOfRangeException("Unhandled key:" + key);
      }


    }
  }

  public class L10PageTypeToTermsKey {
    public static TermKey? GetKey(L10PageType pageType) {
      switch (pageType) {
        case L10PageType.Segue:
          return TermKey.CheckIn;
        case L10PageType.Scorecard:
          return TermKey.Metrics;
        case L10PageType.Rocks:
          return TermKey.Goals;
        case L10PageType.Headlines:
          return TermKey.Headlines;
        case L10PageType.Todo:
          return TermKey.ToDos;
        case L10PageType.IDS:
          return TermKey.Issues;
        case L10PageType.Conclude:
          return TermKey.WrapUp;
        default:
          return null;
      }
    }
  }
}
