using HotChocolate;
using System;

namespace RadialReview.GraphQL.Models
{
  public class HeadlineQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }

    #endregion

    #region Properties

    public long RecurrenceId { get; set; }
    public string Title { get; set; }
    public bool Archived { get; set; }
    public double? ArchivedTimestamp { get; set; }
    public string NotesId { get; set; }
    public string NotesText { get; set; }
    public long UserId { get; set; }
    public UserQueryModel Assignee { get; set; }

    #endregion

    #region Subscription Data

    public static class Associations
    {
      public enum User6
      {
        Assignee
      }

      public enum Meeting10
      {
        Meetings
      }
    }

    public static class Collections
    {
      [Obsolete] // TODO: See: https://winterinternational.atlassian.net/browse/TTD-2331
      public enum Meeting8
      {
        Meetings
      }
    }
  }

  #endregion

}