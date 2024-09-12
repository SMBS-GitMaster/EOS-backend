using HotChocolate.Data.Filters;
using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories {

  public partial interface IDataContext {

    Task<IQueryable<IssueQueryModel>> GetIssuesForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken);

    Task<IQueryable<IssueQueryModel>> GetLongTermIssuesForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken);

    Task<IQueryable<IssueQueryModel>> GetSentToIssuesForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken);

    Task<IQueryable<IssueQueryModel>> GetArchivedIssuesForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken);

    IQueryable<IssueQueryModel> GetSolvedIssuesForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken);

    Task<IQueryable<IssueQueryModel>> GetRecentlySolvedIssuesForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken);

    Task<IQueryable<IssueSentToMeetingDTO>> GetIssuesSentToForMeetingAsync(long meetingId, CancellationToken cancellationToken);

    Task<IQueryable<IssueHistoryEntryQueryModel>> GetIssueHistoryEntriesForIssuesAsync(IEnumerable<long> issueIds, CancellationToken cancellationToken);

    Task<IssueQueryModel> GetIssueByIdAsync(long? issueId, CancellationToken cancellationToken);

    Task<IQueryable<IssueHistoryEntryQueryModel>> GetIssueHistoryEntriesAsync(CancellationToken cancellationToken);

    Task<bool> GetIssueAddToDepartmentPlanAsync(long id, CancellationToken cancellationToken);

    Task<int> GetSolvedIssueCountAsync(long meetingId, long recurrenceId, CancellationToken cancellationToken);

    Task<IQueryable<IdNamePairQueryModel>> GetEditIssueMeetings(long recurrenceId, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext {

    #region Public Methods

    public async Task<IssueQueryModel> GetIssueByIdAsync(long? issueId, CancellationToken cancellationToken) {
      if (issueId == null)
        return null;
      return repository.GetIssueById(issueId.Value, cancellationToken);
    }

    public async Task<IQueryable<IssueQueryModel>> GetIssuesForMeetingsAsync(IEnumerable<long> meetingIds, CancellationToken cancellationToken) {
      return repository.GetIssuesForMeetings(meetingIds, cancellationToken);
    }

    public async Task<IQueryable<IssueQueryModel>> GetSentToIssuesForMeetingsAsync(IEnumerable<long> meetingIds, CancellationToken cancellationToken)
    {
      return repository.GetSenToIssuesForMeetings(meetingIds, cancellationToken);
    }

    public async Task<IQueryable<IssueQueryModel>> GetLongTermIssuesForMeetingsAsync(IEnumerable<long> meetingIds, CancellationToken cancellationToken)
    {
      return repository.GetLongTermIssuesForMeetings(meetingIds, cancellationToken);
    }

    public async Task<IQueryable<IssueQueryModel>> GetArchivedIssuesForMeetingsAsync(IEnumerable<long> meetingIds, CancellationToken cancellationToken)
    {
      return repository.GetArchivedIssuesForMeetings(meetingIds, cancellationToken);
    }

    public IQueryable<IssueQueryModel> GetSolvedIssuesForMeetingsAsync(IEnumerable<long> meetingIds, CancellationToken cancellationToken)
    {
      return repository.GetSolvedIssuesForMeetings(meetingIds, cancellationToken);
    }

    public async Task<IQueryable<IssueQueryModel>> GetRecentlySolvedIssuesForMeetingsAsync(IEnumerable<long> meetingIds, CancellationToken cancellationToken)
    {
      return await repository.GeRecentlytSolvedIssuesForMeetings(meetingIds, cancellationToken);
    }

    public async Task<IQueryable<IssueHistoryEntryQueryModel>> GetIssueHistoryEntriesAsync(CancellationToken cancellationToken) {
      return (repository.IssueHistoryEntries);
    }

    public async Task<IQueryable<IssueHistoryEntryQueryModel>> GetIssueHistoryEntriesForIssuesAsync(IEnumerable<long> issueIds, CancellationToken cancellationToken) {
      return (repository.GetIssueHistoryEntriesForIssues(issueIds, cancellationToken));
    }

    public async Task<IQueryable<IssueSentToMeetingDTO>> GetIssuesSentToForMeetingAsync(long meetingId, CancellationToken cancellationToken) {
      return (repository.GetIssuesSentToForMeeting(meetingId, cancellationToken));
    }

    public async Task<bool> GetIssueAddToDepartmentPlanAsync(long id, CancellationToken cancellationToken) {
      return await repository.GetIssueAddToDepartmentPlan(id, cancellationToken);
    }

    public async Task<int> GetSolvedIssueCountAsync(long meetingId, long recurrenceId, CancellationToken cancellationToken) {
      return (repository.GetSolvedIssueCount(meetingId, recurrenceId, cancellationToken));
    }

    public async Task<IQueryable<IdNamePairQueryModel>> GetEditIssueMeetings(long recurrenceId, CancellationToken cancellationToken) {
      return repository.GetEditIssueMeetings(recurrenceId, cancellationToken);
    }

    #endregion

  }

}
