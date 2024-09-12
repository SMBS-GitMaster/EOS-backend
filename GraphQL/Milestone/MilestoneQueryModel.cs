using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.GraphQL.Models.Query;

namespace RadialReview.GraphQL.Models {
  public class MilestoneQueryModel {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }

    #endregion

    #region Properties

    public double? DateDeleted { get; set; }

    public string Title { get; set; }
    public double DueDate { get; set; }
    public bool Completed { get; set; }
    public long GoalId { get; set; }

    public gqlMilestoneStatus? status { get; set; }

    #endregion

  }
}