using FluentNHibernate.Mapping;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RadialReview.Models;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Models.Terms
{


  public class TermsPluginModel : ILongIdentifiable
  {
    public TermsPluginModel()
    {
      CreateTime = DateTime.UtcNow;
      LanguageCode = "en-us";
    }
    public virtual long Id { get; set; }
    public virtual string LanguageCode { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual string WeeklyMeeting { get; set; }
    public virtual string CheckIn { get; set; }
    public virtual string Metrics { get; set; }
    public virtual string Goals { get; set; }
    public virtual string Headlines { get; set; }
    public virtual string ToDos { get; set; }
    public virtual string Issues { get; set; }
    public virtual string WrapUp { get; set; }
    public virtual string BusinessPlan { get; set; }
    public virtual string DepartmentPlan { get; set; }
    public virtual string FutureFocus { get; set; }
    public virtual string ShortTermFocus { get; set; }
    public virtual string LongTermIssues { get; set; }
    public virtual string OrganizationalChart { get; set; }
    public virtual string OrgChart { get; set; }
    public virtual string CoreValues { get; set; }
    public virtual string Focus { get; set; }
    public virtual string BHAG { get; set; }
    public virtual string MarketingStrategy { get; set; }
    public virtual string Differentiators { get; set; }
    public virtual string ProvenProcess { get; set; }
    public virtual string Guarantee { get; set; }
    public virtual string TargetMarket { get; set; }
    public virtual string Visionary { get; set; }
    public virtual string SecondInCommand { get; set; }
    public virtual string ThreeYearVision { get; set; }
    public virtual string OneYearGoals { get; set; }
    public virtual string LeadAndManage { get; set; }
    public virtual string QuarterlyPlanning { get; set; }
    public virtual string AnnualPlanning { get; set; }
    public virtual string Quarters { get; set; }
    public virtual string EmpowerThroughChoice { get; set; }
    public virtual string Understand { get; set; }
    public virtual string Embrace { get; set; }
    public virtual string Capacity { get; set; }
    public virtual string ThinkOnTheBusiness { get; set; }
    public virtual string Quarterly1_1 { get; set; }
    public virtual string RightPersonRightSeat { get; set; }
    public virtual string QuarterlyGoals { get; set; }
    public virtual string One_OneMeeting { get; set; }
    public virtual string LaunchDay { get; set; }
    public virtual string FutureFocusDay { get; set; }
    public virtual string ShortTermFocusDay { get; set; }
    //public virtual string GoalsRolesCoreValues { get; set; }
    public virtual string PurposeCausePassion { get; set; }
    public virtual string Measurables { get; set; }
    public virtual string Milestones { get; set; }
    public virtual string Niche { get; set; }

    public virtual void ApplyToTermsModel(TermsModel model)
    {
      model.WeeklyMeeting = this.WeeklyMeeting;
      model.CheckIn = this.CheckIn;
      model.Metrics = this.Metrics;
      model.Goals = this.Goals;
      model.Headlines = this.Headlines;
      model.ToDos = this.ToDos;
      model.Issues = this.Issues;
      model.WrapUp = this.WrapUp;
      model.BusinessPlan = this.BusinessPlan;
      model.DepartmentPlan = this.DepartmentPlan;
      model.FutureFocus = this.FutureFocus;
      model.ShortTermFocus = this.ShortTermFocus;
      model.LongTermIssues = this.LongTermIssues;
      model.OrganizationalChart = this.OrganizationalChart;
      model.OrgChart = this.OrgChart;
      model.CoreValues = this.CoreValues;
      model.Focus = this.Focus;
      model.BHAG = this.BHAG;
      model.MarketingStrategy = this.MarketingStrategy;
      model.Differentiators = this.Differentiators;
      model.ProvenProcess = this.ProvenProcess;
      model.Guarantee = this.Guarantee;
      model.TargetMarket = this.TargetMarket;
      model.Visionary = this.Visionary;
      model.SecondInCommand = this.SecondInCommand;
      model.ThreeYearVision = this.ThreeYearVision;
      model.OneYearGoals = this.OneYearGoals;
      model.LeadAndManage = this.LeadAndManage;
      model.QuarterlyPlanning = this.QuarterlyPlanning;
      model.AnnualPlanning = this.AnnualPlanning;
      model.Quarters = this.Quarters;
      model.EmpowerThroughChoice = this.EmpowerThroughChoice;
      model.Understand = this.Understand;
      model.Embrace = this.Embrace;
      model.Capacity = this.Capacity;
      model.ThinkOnTheBusiness = this.ThinkOnTheBusiness;
      model.Quarterly1_1 = this.Quarterly1_1;
      model.RightPersonRightSeat = this.RightPersonRightSeat;
      model.QuarterlyGoals = this.QuarterlyGoals;
      model.One_OneMeeting = this.One_OneMeeting;
      model.LaunchDay = this.LaunchDay;
      model.FutureFocusDay = this.FutureFocusDay;
      model.ShortTermFocusDay = this.ShortTermFocusDay;
      model.PurposeCausePassion = this.PurposeCausePassion;
      model.Measurables = this.Measurables;
      model.Milestones = this.Milestones;
      model.Niche = this.Niche;
    }

    public class Map : ClassMap<TermsPluginModel>
    {
      public Map()
      {
        Id(x => x.Id);
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);

        Map(x => x.WeeklyMeeting);
        Map(x => x.CheckIn);
        Map(x => x.Metrics);
        Map(x => x.Goals);
        Map(x => x.Headlines);
        Map(x => x.ToDos);
        Map(x => x.Issues);
        Map(x => x.WrapUp);
        Map(x => x.BusinessPlan);
        Map(x => x.DepartmentPlan);
        Map(x => x.FutureFocus);
        Map(x => x.ShortTermFocus);
        Map(x => x.LongTermIssues);
        Map(x => x.OrganizationalChart);
        Map(x => x.OrgChart);
        Map(x => x.CoreValues);
        Map(x => x.Focus);
        Map(x => x.BHAG);
        Map(x => x.MarketingStrategy);
        Map(x => x.Differentiators);
        Map(x => x.ProvenProcess);
        Map(x => x.Guarantee);
        Map(x => x.TargetMarket);
        Map(x => x.Visionary);
        Map(x => x.SecondInCommand);
        Map(x => x.ThreeYearVision);
        Map(x => x.OneYearGoals);
        Map(x => x.LeadAndManage);
        Map(x => x.QuarterlyPlanning);
        Map(x => x.AnnualPlanning);
        Map(x => x.Quarters);
        Map(x => x.EmpowerThroughChoice);
        Map(x => x.Understand);
        Map(x => x.Embrace);
        Map(x => x.Capacity);
        Map(x => x.ThinkOnTheBusiness);
        Map(x => x.Quarterly1_1);
        Map(x => x.RightPersonRightSeat);
        Map(x => x.QuarterlyGoals);
        Map(x => x.One_OneMeeting);
        Map(x => x.LaunchDay);
        Map(x => x.FutureFocusDay);
        Map(x => x.ShortTermFocusDay);
        Map(x => x.PurposeCausePassion);
        Map(x => x.Measurables);
        Map(x => x.Milestones);
        Map(x => x.Niche);
      }

    }
  }
}
