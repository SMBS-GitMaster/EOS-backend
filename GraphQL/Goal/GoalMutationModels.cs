using HotChocolate.Types;
using RadialReview.BusinessPlan.Models.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models.Mutations
{
  public class GoalCreateModel
  {

    public string Title { get; set; }

    public double DueDate { get; set; }

    public long Assignee { get; set; }

    [DefaultValue(false)] public bool AddToDepartmentPlan { get; set; }

    [DefaultValue(null)] public List<GoalEditMeetingModel> MeetingsAndPlans { get; set; }

    public string Status { get; set; }

    [DefaultValue(null)] public string NotesId { get; set; }

    public Goal_MilestoneCreateModel[] Milestones { get; set; }

    public class Goal_MilestoneCreateModel
    {
      #region Properties

      public string Title { get; set; }

      public double DueDate { get; set; }
      [DefaultValue(false)] public bool Completed { get; set; }

      public string Status { get; set; }

      #endregion
    }
  }

  public class GoalEditModel
  {
    public long GoalId { get; set; }
    [DefaultValue(null)] public string NotesId { get; set; }
    [DefaultValue(null)] public string Status { get; set; }
    [DefaultValue(null)] public string Title { get; set; }
    public long? Assignee { get; set; }
    public bool? AddToDepartmentPlan { get; set; }
    public bool? Archived { get; set; }
    public double? DueDate { get; set; }
    [DefaultValue(null)] public MilestoneEditModel[] Milestones { get; set; }
    [DefaultValue(null)] public List<GoalEditMeetingModel> MeetingsAndPlans { get; set; }

  }

  public class GoalEditMeetingModel
  {

    public long MeetingId { get; set; }

    public bool? AddToDepartmentPlan { get; set; }

  }
}

