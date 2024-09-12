using RadialReview.GraphQL.Models;
using RadialReview.Models.Issues;
using System.Collections.Generic;

namespace RadialReview.Core.Repositories
{
  public static class IssueTransformer
  {

    #region Public Methods

    public static IssueQueryModel IssueFromIssueRecurrence(this IssueModel.IssueModel_Recurrence source, string notesText = null)
    {

      var issue = new IssueQueryModel()
      {
        Id = source.Id,
        RecurrenceId = source.Recurrence.Id,
        Completed = source.CloseTime != null && source.DeleteTime == null,
        CompletedTimestamp = source.CloseTime.ToUnixTimeStamp(),
        Archived = source.DeleteTime != null,
        ArchivedTimestamp = source.DeleteTime.ToUnixTimeStamp(), 
        Title = source.Issue.Message,
        NotesId = source.Issue.PadId,
        NotesText = notesText,
        NumStarVotes = source.Stars,
        Context = source.Issue.ContextNodeType != null ?
          new ContextModel()
          {
            FromNodeType = source.Issue.ContextNodeType,
            FromNodeTitle = source.Issue.ContextNodeTitle,
          } : null,
        Assignee = UserTransformer.TransformUser(source.Owner),
        IssueNumber = source.Ordering,
        Version = source.Version,
        LastUpdatedBy = source.LastUpdatedBy,
        DateLastModified = source.DateLastModified,
        PriorityVoteRank = source.Rank == 0 ? 999 : source.Rank,
        IssueCompartment = source.IssueCompartment,
        DateCreated = source.CreateTime.ToUnixTimeStamp(),
        SentFromIssueId = source.CopiedFrom == null ? null : source.CopiedFrom.Id,
        sentFromIssueMeetingName = source.FromWhere,
        sentToIssueMeetingName = source._MovedToMeetingName,
        SentToIssueId = source._MovedToIssueId == 0 ? null : source._MovedToIssueId,
        AddToDepartmentPlan = source.AddToDepartmentPlan,
      };

      if (source.MergedIssueData is not null)
      {
        issue.FromMergedIds = source.MergedIssueData.FromMergedIds;
      }

      return issue;
    }

    #endregion

  }
}
