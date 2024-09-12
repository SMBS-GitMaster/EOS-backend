using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    Task<IQueryable<MeetingAttendeeQueryModel>> GetMeetingAttendeesAsync(long recurrenceId, CancellationToken cancellationToken);
    Task<IQueryable<MeetingAttendeeQueryModelLookup>> GetMeetingAttendeesLookupAsync(long recurrenceId, CancellationToken cancellationToken);

    Task<MeetingAttendeeQueryModel> GetCallerAsMeetingAttendee(long recurrenceId, CancellationToken cancellationToken);

    Task<IQueryable<MeetingAttendeeQueryModel>> GetAttendeesByMeetingIdsAsync(List<long> meetingIds, CancellationToken cancellationToken);
    Task<IQueryable<MeetingAttendeeQueryModel>> GetCurrentAttendeesByMeetingIdsAsync(List<long> meetingIds, CancellationToken cancellationToken);
    Task<IQueryable<MeetingAttendeeQueryModel>> GetCurrentAttendeesByMeetingIdsAsync(List<long> meetingIds, long userId, CancellationToken cancellationToken);

    List<MeetingAttendeeQueryModel> GetMeetingAttendeeFromCaller(List<long> meetingIds);

    Task<GraphQLResponseBase> SetMeetingAttendeeIsPresent(MeetingAttendeeIsPresentModel model);
  }

  public partial class DataContext : IDataContext
  {

    public async Task<IQueryable<MeetingAttendeeQueryModel>> GetMeetingAttendeesAsync(long recurrenceId, CancellationToken cancellationToken)
    {
      return await repository.GetMeetingAttendeesAsync(recurrenceId, cancellationToken);
    }

    public Task<IQueryable<MeetingAttendeeQueryModelLookup>> GetMeetingAttendeesLookupAsync(long recurrenceId, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetMeetingAttendeesLookupAsync(recurrenceId, cancellationToken));
    }

    public Task<MeetingAttendeeQueryModel> GetCallerAsMeetingAttendee(long recurrenceId, CancellationToken cancellationToken)
    {
      return repository.GetCallerAsMeetingAttendee(recurrenceId, cancellationToken);
    }

    public Task<IQueryable<MeetingAttendeeQueryModel>> GetAttendeesByMeetingIdsAsync(List<long> meetingIds, CancellationToken cancellationToken)
    {
      return repository.GetAttendeesByMeetingIds(meetingIds, cancellationToken);
    }

    public Task<IQueryable<MeetingAttendeeQueryModel>> GetCurrentAttendeesByMeetingIdsAsync(List<long> meetingIds, CancellationToken cancellationToken)
    {
      return repository.GetCurrentAttendeesByMeetingIds(meetingIds, cancellationToken);
    }

    public Task<IQueryable<MeetingAttendeeQueryModel>> GetCurrentAttendeesByMeetingIdsAsync(List<long> meetingIds, long userId, CancellationToken cancellationToken)
    {
      return repository.GetCurrentAttendeesByMeetingIds(meetingIds, userId, cancellationToken);
    }

    public Task<GraphQLResponseBase> SetMeetingAttendeeIsPresent(MeetingAttendeeIsPresentModel model)
    {
      return repository.SetMeetingAttendeeIsPresent(model);
    }

    public List<MeetingAttendeeQueryModel> GetMeetingAttendeeFromCaller(List<long> meetingIds)
    {
      return repository.GetMeetingAttendeeFromCaller(meetingIds);
    }
  }
}