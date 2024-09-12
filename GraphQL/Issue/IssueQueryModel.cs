using System;
using System.Collections.Generic;
using static RadialReview.Accessors.IssuesAccessor;

namespace RadialReview.GraphQL.Models
{
  public class IssueQueryModel
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

    public bool Completed { get; set; }

    public double? CompletedTimestamp { get; set; }

    public bool Archived { get; set; }

    public double? ArchivedTimestamp { get; set; }

    public string Title { get; set; }

    public string NotesId { get; set; }
    public string NotesText { get; set; }

    public ContextModel Context { get; set; }

    public UserQueryModel Assignee { get; set; }

    public bool AddToDepartmentPlan { get; set; }
    
    public int NumStarVotes { get; set; }

    public long? IssueNumber { get; set; }

    public int Interval { get; set; }

    public int? PriorityVoteRank { get; set; }

    public List<long> FromMergedIds { get; set; }

    public IssueCompartment? IssueCompartment { get; set; }

    public long? SentFromIssueId { get; set; }

    public string sentFromIssueMeetingName { get; set; }

    public string sentToIssueMeetingName  { get; set; }

    public long? SentToIssueId { get; set; }

    public IssueQueryModel SentToIssue { get; set; }

    #endregion

    public static class Collections 
    {
      public enum IssueSentTo 
      {
        IssueSetTo
      }

      public enum IssueHistoryEntry 
      {
        IssueHistoryEntries
      }

      public enum Comment3
      {
        Comments
      }
    }

    public static class Associations
    {
      public enum User14
      {
        Assignee
      }

      public enum Meeting 
      {
        Meeting
      }

      public enum Issue2
      {
        Issue,
        LongTermIssue
      }

    }
  }
}