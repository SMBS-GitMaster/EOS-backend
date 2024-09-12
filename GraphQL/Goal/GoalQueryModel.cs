using RadialReview.Core.GraphQL.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.GraphQL.Models
{
  public class GoalQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public long RecurrenceId { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }

    #endregion

    #region Properties

    public string Title { get; set; }
    public string NotesId { get; set; }
    public gqlGoalStatus Status { get; set; }
    public double? DueDate { get; set; }
    public bool AddToDepartmentPlan { get; set; }
    public bool Archived { get; set; }
    public double? ArchivedTimestamp { get; set; }
    public UserQueryModel Assignee { get; set; }

    public IQueryable<DepartmentPlanRecordQueryModel> DepartmentPlanRecords { get; set; } = Enumerable.Empty<DepartmentPlanRecordQueryModel>().AsQueryable();

    #endregion

    #region Subscription Data

    public static class Collections
    {
      public enum Milestone
      {
        Milestones
      }

      public enum DepartmentPlanRecord
      {
        DepartmentPlanRecords
      }

      public enum Meeting4
      {
        Meetings
      }
    }

    public static class Associations
    {
      public enum User5
      {
        Assignee
      }
    }


    #endregion

  }
}