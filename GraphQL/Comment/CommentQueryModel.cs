using System;

namespace RadialReview.GraphQL.Models
{
  public class CommentQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLateModified { get; set; }
    public double LateUpdatedClientTimestemp { get; set; }
    public string Type { get { return "comment"; } }

    #endregion

    #region Properties
    public long AuthorId { get; set; }
    public string Body { get; set; }
    public RadialReview.Models.ParentType CommentParentType { get; set; } //!! This should be gqlParentType
    public long ParentId { get; set; }
    public double PostedTimestamp { get; set; }
    public bool Archived { get; set; }
    public double? ArchivedDate { get; set; }

    #endregion

    #region Subscription Data

    public static class Associations 
    {
      public enum User 
      {
        Author
      }
    }

    #endregion

  }
}