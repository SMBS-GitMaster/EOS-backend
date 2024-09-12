using System.Collections.Generic;

namespace RadialReview.GraphQL.Models
{
  public class TodoQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }

    #endregion

    #region Properties

    public string Title { get; set; }

    public double? DueDate { get; set; }

    public bool Completed { get; set; }

    public double? CompletedTimestamp { get; set; }

    public bool Archived { get; set; }

    public double? ArchivedTimestamp { get; set; }

    public UserQueryModel Assignee { get; set; }

    public List<CommentQueryModel> Comments { get; set; }

    public string NotesId { get; set; }
    public string NotesText { get; set; }

    public long ForRecurrenceId { get; set; }

    public ContextModel Context { get; set; }

    #endregion

    public static class Associations 
    {
      public enum User2 
      {
        Assignee
      }

      public enum Meeting5
      {
        Meeting
      }
    }

    public static class Collections 
    {
      public enum Comment2 
      {
        Comments
      }
    }
  }
}