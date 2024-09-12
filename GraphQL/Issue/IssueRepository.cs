using DocumentFormat.OpenXml.Office2010.Excel;
using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.Repositories;
using RadialReview.Crosscutting.Hooks;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.Issues;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries

    Task<bool> GetIssueAddToDepartmentPlan(long id, CancellationToken cancellationToken);

    IssueQueryModel GetIssueById(long issueId, CancellationToken cancellationToken);

    IQueryable<IssueHistoryEntryQueryModel> GetIssueHistoryEntriesForIssues(IEnumerable<long> issueIds, CancellationToken cancellationToken);

    IQueryable<IssueHistoryEntryQueryModel> IssueHistoryEntries { get; }

    IQueryable<IssueQueryModel> GetIssuesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken);

    IQueryable<IssueQueryModel> GetLongTermIssuesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken);

    IQueryable<IssueQueryModel> GetSenToIssuesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken);

    IQueryable<IssueQueryModel> GetArchivedIssuesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken);

    IQueryable<IssueQueryModel> GetSolvedIssuesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken);

    Task<IQueryable<IssueQueryModel>> GeRecentlytSolvedIssuesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken);

    IQueryable<IssueSentToMeetingDTO> GetIssuesSentToForMeeting(long meetingId, CancellationToken cancellationToken);

    int GetSolvedIssueCount(long meetingId, long recurrenceId, CancellationToken cancellationToken);

    IQueryable<IdNamePairQueryModel> GetEditIssueMeetings(long recurrenceId, CancellationToken cancellationToken);

    Task<GraphQLResponse<bool>> ResetPriorityVoting(long meetingId);

    #endregion

    #region Mutations

    Task<long> AssignIssueToMeeting(long issueId, long recurrenceId, CancellationToken cancellationToken);

    Task<IssueQueryModel> CreateIssue(IssueCreateModel issue);

    Task<GraphQLResponse<bool>> EditIssue(IssueEditModel issue);

    Task<long> ReassignIssueToMeeting(long issueId, long oldRecurrenceId, long newRecurrenceId, CancellationToken cancellationToken);

    Task<GraphQLResponseBase> ResetIssueStarVoting(long meetingId);

    Task<GraphQLResponseBase> SubmitIssueStarVotes(IssueSubmitStarVotesModel model);

    Task<GraphQLResponseBase> SubmitIssuePriorityVotes(IssueSubmitPriorityVotesModel model);

    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public Task<bool> GetIssueAddToDepartmentPlan(long id, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          //PermissionsUtility.Create(s, caller).ViewIssueRecurrence(id);
          //PermissionsUtility.Create(s, caller).ViewIssue(id);
          var vtoIssue = s.QueryOver<VtoItem_String>()
            .Where(x => x.Type == VtoItemType.List_Issues && x.ForModel.ModelId == id && x.DeleteTime == null)
            .List()
            .FirstOrDefault();
          //var res = s.QueryOver<VtoItem>().Where(x => x.ForModel.ModelId == id && x.ForModel.ModelType == "RadialReview.Models.Issues.IssueModel+IssueModel_Recurrence").List().FirstOrDefault();
          return Task.FromResult(vtoIssue != null);
        }
      }
    }

    public IssueQueryModel GetIssueById(long issueId, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {

        var issue = IssuesAccessor.GetIssue_Recurrence(caller, issueId, false, true);
        return IssueTransformer.IssueFromIssueRecurrence(issue);

        //throw new Exception("This method needs to check for permissions");
        //var issue = s.QueryOver<Models.Issues.IssueModel.IssueModel_Recurrence>().Where(x => x.Id == issueId).Take(1).List().FirstOrDefault();
        //return RepositoryTransformers.IssueFromIssueRecurrence(issue);
      }
    }

    public IQueryable<IssueHistoryEntryQueryModel> GetIssueHistoryEntriesForIssues(IEnumerable<long> issueIds, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        return IssuesAccessor.GetVisibleIssueHistoryEntries(caller, issueIds.ToArray());
      });
    }

    public IQueryable<IssueHistoryEntryQueryModel> IssueHistoryEntries
    {
      get
      {
        return new List<IssueHistoryEntryQueryModel>().AsQueryable();
      }
    }

    public IQueryable<IssueQueryModel> GetIssuesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken)
    {
      var issues = L10Accessor.GetIssuesForRecurrences(caller, meetingIds.ToList());
      return issues.Select(ir => IssueTransformer.IssueFromIssueRecurrence(ir)).AsQueryable();
    }

    public IQueryable<IssueQueryModel> GetSenToIssuesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken)
    {
      var sentToIssues = L10Accessor.GetSentToIssuesForRecurrences(caller, meetingIds.ToList());
      return sentToIssues.Select(ir =>
      {
        var issue = ir.IssueFromIssueRecurrence();
        if (ir._SentToIssue is not null)
          issue.SentToIssue = ir._SentToIssue.IssueFromIssueRecurrence();

        return issue;
      }).AsQueryable();
    }

    public IQueryable<IssueQueryModel> GetLongTermIssuesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken)
    {
      var issuesVto = VtoAccessor.GetAllVTOIssueByRecurrenceIds(caller, meetingIds.ToList());
      return issuesVto.AsQueryable();
    }

    public IQueryable<IssueQueryModel> GetArchivedIssuesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken)
    {
      var archivedIssues = L10Accessor.GetArchivedIssuesForRecurrences(caller, meetingIds.ToList());
      return archivedIssues.Select(ir => ir.IssueFromIssueRecurrence()).AsQueryable();
    }

    public IQueryable<IssueQueryModel> GetSolvedIssuesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken)
    {
      var solvedIssues = L10Accessor.GetSolvedIssuesForRecurrences(caller, meetingIds.ToList());
      return solvedIssues.Select(ir => ir.IssueFromIssueRecurrence()).AsQueryable();
    }

    public async Task<IQueryable<IssueQueryModel>> GeRecentlytSolvedIssuesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken)
    {
      var recent = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
      var recentlySolvedIssues = L10Accessor.GetSolvedIssuesForRecurrences(caller, meetingIds.ToList(), recent);
      var padIds = recentlySolvedIssues.Select(x => x.Issue.PadId).Where(x => x != null);
      var padTexts = await _notesProvider.GetHtmlForPads(padIds);

      return recentlySolvedIssues.Select(x =>
      {
        string notesText = "";

        if (x.Issue.PadId != null && padTexts.ContainsKey(x.Issue.PadId))
        {
          notesText = padTexts[x.Issue.PadId].ToString();
        }

        return x.IssueFromIssueRecurrence(notesText);

      }).AsQueryable();
    }

    public IQueryable<IssueSentToMeetingDTO> GetIssuesSentToForMeeting(long meetingId, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom<IssueSentToMeetingDTO>(cancellationToken, () =>
      {
        //throw new Exception("Permissions checks needed");
        return IssuesAccessor.GetIssuesSentToForRecurrence(caller, meetingId);
      });
    }

    public int GetSolvedIssueCount(long meetingId, long recurrenceId, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          //Todo move the permissions and the call into an accessor
          var perms = PermissionsUtility.Create(s, caller);
          //perms.ViewL10Meeting(meetingId); //!! Meeting Id is not being correct at the moment it is actually recurrence id. care later
          perms.ViewL10Recurrence(recurrenceId);
          var conclusion = L10Accessor.ConclusionItems.Get_Unsafe(s, meetingId, recurrenceId);
          if (conclusion == null || conclusion.ClosedIssues == null)
            return 0;
          return conclusion.ClosedIssues.Count;
        }
      }
    }

    private IQueryable<IssueQueryModel> GetAllIssuesForMeetingRecurrence(long meetingId, bool includeArchived, bool includeLongTerm, bool includeSentTo, CancellationToken cancellationToken)
    {

      var combinedIssues = new List<IssueQueryModel>();

      var issues = L10Accessor.GetIssuesForRecurrence(caller, meetingId, includeArchived, includeArchived);
      if (includeSentTo) {
        //not sure if this is right...
        issues = IssuesAccessor.GetIssues_MovedToFix(issues);
      }
      var issuesConverted = issues.Select(i => IssueTransformer.IssueFromIssueRecurrence(i)).ToList();
      combinedIssues.AddRange(issuesConverted);

      if (includeLongTerm)
      {
        var issuesVto = VtoAccessor.GetAllVTOIssue(caller, meetingId);
        combinedIssues.AddRange(issuesVto);
      }

      return combinedIssues.AsQueryable();

    }

    public IQueryable<IdNamePairQueryModel> GetEditIssueMeetings(long recurrenceId, CancellationToken cancellationToken)
    {
      return L10Accessor.GetAllConnectedL10Recurrence(caller, recurrenceId, true, true)
        .Select(x => RepositoryTransformers.RecurrenceIdNamePair(x)).ToList().AsQueryable();
    }

    private IQueryable<IssueQueryModel> GetIssuesForMeetingRecurrance(long recurrenceId, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        //throw new Exception("Shouldnt need to use a requester, use caller instead");
        return L10Accessor.GetIssuesForRecurrence(caller, recurrenceId, false /*changed to false*/)
                  .Select(i => IssueTransformer.IssueFromIssueRecurrence(i))
                  .ToList();
      });
    }

    #endregion

    #region Mutations

    public async Task<long> AssignIssueToMeeting(long issueId, long recurrenceId, CancellationToken cancellationToken)
    {
      //!! This method does nothing
      throw new Exception("This method fails to check permissions for issue id");
      throw new Exception("This method fails to check permissions for recurrenceId.");
      //using (var tx = session.BeginTransaction()) {
      //  var now = DateTime.UtcNow;

      //  var newHistoryEntry =
      //      new Models.Issues.IssueHistoryEntry() {
      //        CreateTime = now,
      //        ValidFrom = now,
      //        ValidUntil = null,

      //        Issue = session.Get<Models.Issues.IssueModel>(issueId),
      //        Meeting = session.Get<Models.L10.L10Recurrence>(recurrenceId),
      //      };
      //  await session.SaveAsync(newHistoryEntry, cancellationToken);

      //  tx.Commit();
      //  session.Flush();

      //  return newHistoryEntry.Id;
    }

    public async Task<IssueQueryModel> CreateIssue(IssueCreateModel issue)
    {
      string contextTitle = issue.Context == null ? null : issue.Context.FromNodeTitle;
      string contextType = issue.Context == null ? null : issue.Context.FromNodeType;

      var mergedIssueDataBuilder = new MergedIssueData.Builder();
      var mergedIssueData = mergedIssueDataBuilder
                              .SetFromMergedIssueIds(issue.FromMergedIds)
                              .Build();

      var creation = IssueCreation.CreateL10Issue(issue.Title, issue.Notes, issue.OwnerId, issue.RecurrenceId, padId: issue.NotesId, contextTitle: contextTitle, contextType: contextType, addToDepartmentPlan: issue.AddToDepartmentPlan, MergedIssueData: mergedIssueData);
      var success = await IssuesAccessor.CreateIssue(caller, creation, issue.NotesId != null);

      var output = IssueTransformer.IssueFromIssueRecurrence(success.IssueRecurrenceModel);
      return output;
    }

    public async Task<GraphQLResponse<bool>> EditIssue(IssueEditModel issue)
    {
      try
      {
        await IssuesAccessor.EditIssue(caller, issue.Id, issue.Title, issue.Completed, issue.AssigneeId,
                                        issue.NumStarVotes, issue.PriorityVoteRank, false, compartment: issue.IssueCompartment, noteId: issue.NotesId,
                                        archived: issue.Archived, addToDepartmentPlan:issue.AddToDepartmentPlan);
        var issueRecurrence = IssuesAccessor.GetIssue_Recurrence(caller, issue.Id);
        if (issueRecurrence.Recurrence != null && issue.MeetingId.HasValue && issueRecurrence.Id != issue.MeetingId)
        {
          var dto = new IssueCreateSentToModel { IssueId = issueRecurrence.Id, RecurrenceId = issue.MeetingId.Value };
          await CreateIssueSentTo(dto);
        }
        
        return GraphQLResponse<bool>.Successfully(true);
      }
      catch (Exception ex)
      {
        return GraphQLResponse<bool>.Error(ex);
      }
    }

    public async Task<long> ReassignIssueToMeeting(long issueId, long oldRecurrenceId, long newRecurrenceId, CancellationToken cancellationToken)
    {
      //!! This method does nothing
      throw new Exception("This method fails to check permissions");
      //using (var tx = session.BeginTransaction()) {
      //  var now = DateTime.UtcNow;
      //  var issue = session.Get<Models.Issues.IssueModel>(issueId);
      //  var oldMeeting = session.Get<Models.L10.L10Recurrence>(oldRecurrenceId);
      //  var newMeeting = session.Get<Models.L10.L10Recurrence>(newRecurrenceId);

      //  var oldHistoryEntry = session.Query<Models.Issues.IssueHistoryEntry>().Where(entry => entry.Meeting.Id == oldRecurrenceId && entry.Issue.Id == issueId && entry.ValidUntil == null).SingleOrDefault();
      //  oldHistoryEntry.ValidUntil = now;
      //  await session.UpdateAsync(oldHistoryEntry, cancellationToken);

      //  var newHistoryEntry =
      //      new Models.Issues.IssueHistoryEntry() {
      //        CreateTime = now,
      //        ValidFrom = now,
      //        ValidUntil = null,

      //        Issue = issue,
      //        Meeting = newMeeting,
      //      };
      //  await session.SaveAsync(newHistoryEntry, cancellationToken);

      //  var meet =
      //      session.Query<Models.Issues.IssueModel.IssueModel_Recurrence>()
      //        .Where(x => x.Issue.Id == issueId && x.Recurrence.Id == oldRecurrenceId)
      //        .Single();

      //  meet.Recurrence = newMeeting;
      //  await session.SaveAsync(meet, cancellationToken);

      //  tx.Commit();
      //  session.Flush();

      //  return newHistoryEntry.Id;
    }

    public async Task<GraphQLResponseBase> ResetIssueStarVoting(long meetingId)
    {
      await L10Accessor.ResetStarVoting(caller, meetingId);

      return GraphQLResponseBase.Successfully();
    }

    public async Task<GraphQLResponse<bool>> ResetPriorityVoting(long meetingId)
    {
      try
      {
        await IssuesAccessor.ResetPriorityByRank(caller, meetingId);
        return GraphQLResponse<bool>.Successfully(true);
      }
      catch (Exception ex)
      {
        return GraphQLResponse<bool>.Error(ex);
      }

    }

    public async Task<GraphQLResponseBase> SubmitIssueStarVotes(IssueSubmitStarVotesModel model)
    {
      // Set has voted
      await L10Accessor.EditAttendeeHasVoted(caller, model.RecurrenceId, true);

      // Increment votes
      foreach (var voting in model.Votes)
      {
        await IssuesAccessor.EditIssueVotes(caller, model.RecurrenceId, voting.IssueId, voting.NumberOfVotes);
      }

      return GraphQLResponseBase.Successfully();
    }

    public async Task<GraphQLResponseBase> SubmitIssuePriorityVotes(IssueSubmitPriorityVotesModel model)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          int expectedRank = 1;

          var currentRanks = L10Accessor.GetIssuesForRecurrence(caller, model.MeetingId, false).Where(_ => _.Rank > 0 && _._MovedToIssueId == 0).OrderBy(_ => _.Rank).ToList();

          // Check for exceeding vote count
          if(currentRanks.Count >= 3 && model.VoteTimestamp != null)
          {
            return GraphQLResponseBase.Error(new ErrorDetail("MAX_RANKS_EXCEEDED", GraphQLErrorType.Forbidden));
          }

          // Check if we've already ranked this issue
          if(model.VoteTimestamp != null && currentRanks.Where(_ => _.Id == model.IssueId).Any())
          {
            return GraphQLResponseBase.Error(new ErrorDetail("ISSUE_ALREADY_RANKED", GraphQLErrorType.Forbidden));
          }

          // Check for unranking
          if(model.VoteTimestamp == null)
          {
            // Start by unranking current
            await IssuesAccessor.EditIssue(caller, model.IssueId, rank: 0);

            // Get other ranks that may need to change
            // Won't need this after we get rid of V1
            var otherIssues = currentRanks.Where(_ => _.Id != model.IssueId).ToList();
            foreach(var issue in otherIssues)
            {
              if(issue.Rank != expectedRank)
              {
                // Start by unranking current
                await IssuesAccessor.EditIssue(caller, issue.Id, rank: expectedRank);
              }
              expectedRank += 1;
            }

            // Return success
            return GraphQLResponseBase.Successfully();
          }

          // Check for new ranking
          // Again we're going to loop through existing ones (just in case)
          // Can be removed after V1 goes away
          expectedRank = 1;
          foreach (var issue in currentRanks)
          {
            if (issue.Rank != expectedRank)
            {
              // Start by unranking current
              await IssuesAccessor.EditIssue(caller, issue.Id, rank: expectedRank);
            }
            expectedRank += 1;
          }

          // Finally assign the next expectedRank
          await IssuesAccessor.EditIssue(caller, model.IssueId, rank: expectedRank);
        }
      }


      //// Increment votes
      //foreach (var model in models)
      //{
      //  await IssuesAccessor.EditIssue(caller, model.IssueId, rank: model.Rank);
      //}

      return GraphQLResponseBase.Successfully();
    }

    public async Task<GraphQLResponse<bool>> CreateIssueSentTo(IssueCreateSentToModel model)
    {
      try
      {
        var destIssue = await IssuesAccessor.CopyIssue(caller, model.IssueId, model.RecurrenceId, true);

        var sourceIssue = IssuesAccessor.GetIssue_Recurrence(caller, model.IssueId, false, true);
        await HooksRegistry.Each<IIssueHook>((ses, x) => x.SendIssueTo(ses, caller, sourceIssue, destIssue));

        return GraphQLResponse<bool>.Successfully(true);
      }
      catch (Exception ex)
      {
        return GraphQLResponse<bool>.Error(ex);
      }
    }
    #endregion

  }

}
