using HotChocolate;
using System;

namespace RadialReview.GraphQL.Models
{
  public class TermsQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }
    public double? LastUpdatedClientTimestamp { get; set; }

    #endregion

    #region Properties

    public string WeeklyMeeting { get; set; }
    public string CheckIn { get; set; }
    public string Metrics { get; set; }
    public string Goals { get; set; }
    public string Headlines { get; set; }
    public string ToDos { get; set; }
    public string Issues { get; set; }
    public string WrapUp { get; set; }
    public string BusinessPlan { get; set; }
    public string DepartmentPlan { get; set; }
    public string FutureFocus { get; set; }
    public string ShortTermFocus { get; set; }
    public string LongTermIssues { get; set; }
    public string OrganizationalChart { get; set; }
    public string OrgChart { get; set; }
    public string CoreValues { get; set; }
    public string Focus { get; set; }
    public string BHAG { get; set; }
    public string MarketingStrategy { get; set; }
    public string Differentiators { get; set; }
    public string ProvenProcess { get; set; }
    public string Guarantee { get; set; }
    public string TargetMarket { get; set; }
    public string Visionary { get; set; }
    public string SecondInCommand { get; set; }
    public string ThreeYearVision { get; set; }
    public string OneYearGoals { get; set; }
    public string LeadAndManage { get; set; }
    public string QuarterlyPlanning { get; set; }
    public string AnnualPlanning { get; set; }
    public string Quarters { get; set; }
    public string EmpowerThroughChoice { get; set; }
    public string Understand { get; set; }
    public string Embrace { get; set; }
    public string Capacity { get; set; }
    public string ThinkOnTheBusiness { get; set; }
    public string Quarterly1_1 { get; set; }
    public string RightPersonRightSeat { get; set; }
    public string QuarterlyGoals { get; set; }
    public string One_OneMeeting { get; set; }
    public string LaunchDay { get; set; }
    public string FutureFocusDay { get; set; }
    public string ShortTermFocusDay { get; set; }
    //publal string GoalsRolesCoreValues { get; set; }
    public string PurposeCausePassion { get; set; }
    public string Measurables { get; set; }
    public string Milestones { get; set; }
    public string Niche { get; set; }

    #endregion

  }
}