using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {
    Task<MeetingInstanceAttendeeQueryModel> GetInstanceFromMeetingAttendeeAsync(long userId, long recurrenceId, CancellationToken cancellationToken);

    Task<IQueryable<MeetingInstanceAttendeeQueryModel>> GetMeetingInstanceAttendees(long recurrenceId, IQueryable<MeetingAttendeeQueryModel> attendees, CancellationToken ct);

    Task<IQueryable<MeetingInstanceAttendeeQueryModel>> GetMeetingInstanceAttendees(long recurrenceId, long meetingId, CancellationToken ct);

    Task<IQueryable<MeetingRatingModel>> GetMeetingAttendeeInstancesForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public async Task<MeetingInstanceAttendeeQueryModel> GetInstanceFromMeetingAttendeeAsync(long userId, long recurrenceId, CancellationToken cancellationToken)
    {
      return repository.GetInstanceFromMeetingAttendee(userId, recurrenceId, cancellationToken);
    }

    public Task<IQueryable<MeetingInstanceAttendeeQueryModel>> GetMeetingInstanceAttendees(long recurrenceId, IQueryable<MeetingAttendeeQueryModel> attendees, CancellationToken cancellationToken)
    {
      return (repository.GetMeetingInstanceAttendees(recurrenceId, attendees, cancellationToken));
    }

    public Task<IQueryable<MeetingInstanceAttendeeQueryModel>> GetMeetingInstanceAttendees(long recurrenceId, long meetingId, CancellationToken cancellationToken)
    {
      return (repository.GetMeetingInstanceAttendees(recurrenceId, meetingId, cancellationToken));
    }

    public Task<IQueryable<MeetingRatingModel>> GetMeetingAttendeeInstancesForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetMeetingAttendeeInstancesForMeetings(recurrenceIds, cancellationToken));
    }

  }

}