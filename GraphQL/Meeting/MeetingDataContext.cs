using RadialReview.Core.GraphQL;
using RadialReview.Core.GraphQL.Common;
using RadialReview.Core.GraphQL.MeetingListLookup;
using RadialReview.GraphQL.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    #region Queries

    Task<IQueryable<MeetingQueryModel>> GetMeetingsForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken);

    Task<IQueryable<MeetingQueryModel>> GetMeetingsForUserAsync(CancellationToken cancellationToken);

    Task<IQueryable<MeetingListLookupModel>> GetMeetingsListLookupForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken);

    Task<IQueryable<MeetingQueryModel>> GetMeetingsForRecurrencesAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken);


    Task<IQueryable<MeetingQueryModel>> GetMeetingsForRecurrencesAsync(IEnumerable<long> recurrenceIds, LoadMeetingModel loadMeeting, CancellationToken cancellationToken);


    Task<IQueryable<MeetingQueryModel>> GetMeetingsAsync(IEnumerable<long> ids, CancellationToken cancellationToken);

    Task<IQueryable<MeetingQueryModel>> GetFastMeetingsAsync(IEnumerable<long> ids, CancellationToken cancellationToken);

    Task<MeetingQueryModel> GetMeetingAsync(long? id, CancellationToken cancellationToken);

    Task<MeetingQueryModel> GetMeetingAsync(long? id, LoadMeetingModel loadMeetingModel, CancellationToken cancellationToken);

    Task<IQueryable<MeetingQueryModel>> GetMeetingsForGoalsAsync(IEnumerable<long> rockIds, CancellationToken cancellationToken);

    Task<IQueryable<GoalMeetingQueryModel>> GetGoalMeetingsAsync(IEnumerable<long> rockIds, CancellationToken cancellationToken);

    Task<IQueryable<(long measurableId, MeetingQueryModel meeting)>> GetMeetingsForMetricsAsync(IEnumerable<long> measurableId, CancellationToken cancellationToken);

    Task<MeetingPermissionsModel> GetPermissionsForCallerOnMeetingAsync(long recurrenceId);

    [Obsolete("slow, use GetMeetingMetadataForUsersAsync instead", DebugConst.COMPILE_TIME_ERROR_ON_SLOW_QUERY)]
    Task<IQueryable<MeetingMetadataModel>> GetMeetingMetadataForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken);

    Task<IQueryable<double>> GetAverageMeetingRatingAsync(long recurrenceId, CancellationToken cancellationToken);

    Task<IQueryable<MeetingPageQueryModel>> GetPagesByMeetingIdsAsync(List<long> meetingIds, CancellationToken cancellationToken);

    Task<IQueryable<MeetingQueryModel>> GetMeetingsByIds(IEnumerable<long> meetingIds, LoadMeetingModel loadMeetingModel, CancellationToken cancellationToken);

    Task<IQueryable<(long measurableId, MeetingQueryModel meeting)>> GetMeetingsForMetrics(IEnumerable<long> measurableIds, CancellationToken cancellationToken);
    Task<IQueryable<MeetingModeModel>> GetMeetingModes(CancellationToken cancellationToken);

    #endregion

  }

  public partial class DataContext : IDataContext
  {

    #region Queries

    public async Task<IQueryable<MeetingQueryModel>> GetMeetingsForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      return repository.GetMeetingsForUsers(userIds, cancellationToken);
    }

    public async Task<IQueryable<MeetingListLookupModel>> GetMeetingsListLookupForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      return repository.GetMeetingsListLookupForUsers(userIds, cancellationToken);
    }

    public async Task<IQueryable<MeetingQueryModel>> GetMeetingsForUserAsync(CancellationToken cancellationToken)
    {
      return repository.GetMeetingsForUser(cancellationToken);
    }

    public async Task<IQueryable<MeetingQueryModel>> GetMeetingsForRecurrencesAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken)
    {
        return repository.GetMeetingsByRecurrences(recurrenceIds, cancellationToken);
      }

    public async Task<IQueryable<MeetingQueryModel>> GetMeetingsForRecurrencesAsync(IEnumerable<long> recurrenceIds, LoadMeetingModel loadMeetingModel, CancellationToken cancellationToken)
    {
      return repository.GetMeetingsByRecurrences(recurrenceIds, loadMeetingModel, cancellationToken);
    }

    public Task<IQueryable<(long measurableId, MeetingQueryModel meeting)>> GetMeetingsForMetricsAsync(IEnumerable<long> measurableIds, CancellationToken cancellationToken)
    {
      ConcurrentBag<(long, MeetingQueryModel)> results = new ConcurrentBag<(long measurableId, MeetingQueryModel meeting)>();

      Parallel.ForEach(measurableIds, measurableId =>
      {
        var meetings = repository.GetMeetingsForMetric(measurableId, cancellationToken);

        foreach (var meeting in meetings) {
          results.Add((measurableId, meeting));
        }
      });

      return Task.FromResult(results.AsQueryable());
    }

    public Task<IQueryable<(long measurableId, MeetingQueryModel meeting)>> GetMeetingsForMetrics(IEnumerable<long> measurableIds, CancellationToken cancellationToken)
    {
      return repository.GetRecurrenceForMetric(measurableIds, cancellationToken);
    }

    public async Task<MeetingQueryModel> GetMeetingAsync(long? id, CancellationToken cancellationToken)
    {
      return repository.GetMeeting(id, cancellationToken);
    }

    public async Task<MeetingQueryModel> GetMeetingAsync(long? id, LoadMeetingModel loadMeetingModel, CancellationToken cancellationToken)
    {
      return repository.GetMeeting(id,loadMeetingModel, cancellationToken);
    }

    public async Task<IQueryable<MeetingQueryModel>> GetMeetingsAsync(IEnumerable<long> ids, CancellationToken cancellationToken)
    {
      return repository.GetMeetings(ids, cancellationToken);
    }

    public async Task<IQueryable<MeetingQueryModel>> GetFastMeetingsAsync(IEnumerable<long> ids, CancellationToken cancellationToken)
    {
      return repository.GetMeetingsFast(ids, cancellationToken);
    }

    public async Task<IQueryable<MeetingModeModel>> GetMeetingModes(CancellationToken cancellationToken)
    {
      return repository.GetMeetingModes(cancellationToken);
    }

    public async Task<IQueryable<MeetingQueryModel>> GetMeetingsForGoalsAsync(IEnumerable<long> rockIds, CancellationToken cancellationToken)
    {
      return repository.GetMeetingsForGoals(rockIds, cancellationToken);
    }

    public Task<IQueryable<GoalMeetingQueryModel>> GetGoalMeetingsAsync(IEnumerable<long> rockIds, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetGoalMeetings(rockIds, cancellationToken));
    }

    public Task<MeetingPermissionsModel> GetPermissionsForCallerOnMeetingAsync(long recurrenceId)
    {
      return Task.FromResult(repository.GetPermissionsForCallerOnMeeting(recurrenceId));
    }

    public async Task<IQueryable<MeetingMetadataModel>> GetMeetingMetadataForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      return repository.GetMeetingMetadataForUsers(userIds, cancellationToken);
    }

    public Task<IQueryable<double>> GetAverageMeetingRatingAsync(long recurrenceId, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.AverageMeetingRating(recurrenceId, cancellationToken));
    }

    public Task<IQueryable<MeetingPageQueryModel>> GetPagesByMeetingIdsAsync(List<long> meetingIds, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetPagesByMeetingIds(meetingIds, cancellationToken));
    }

    public Task<IQueryable<MeetingQueryModel>> GetMeetingsByIds(IEnumerable<long> meetingIds, LoadMeetingModel loadMeetingModel, CancellationToken cancellationToken)
    {
      var result = repository.GetMeetingsByIds(meetingIds, loadMeetingModel, cancellationToken);
      return Task.FromResult(result);
    }

    #endregion

  }

}
