using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    Task<MeetingInstanceQueryModel> GetInstanceForMeetingAsync(long? instanceId, long? recurrenceId, CancellationToken cancellationToken);

    Task<IQueryable<MeetingInstanceQueryModel>> GetInstancesForMeetingsAsync(IEnumerable<long> meetingIds, CancellationToken cancellationToken);

    Task<MeetingInstanceQueryModel> GetInstanceForMeetingByRecurrenceIdAsync(long recurrenceId, CancellationToken cancellationToken);
  }

  public partial class DataContext : IDataContext
  {

    public async Task<MeetingInstanceQueryModel> GetInstanceForMeetingAsync(long? instanceId, long? recurrenceId, CancellationToken cancellationToken)
    {
      return repository.GetInstanceForMeeting(instanceId, recurrenceId, cancellationToken);
    }

    public async Task<IQueryable<MeetingInstanceQueryModel>> GetInstancesForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken)
    {
      return repository.GetInstancesForMeetings(recurrenceIds, cancellationToken);
    }

    public async Task<MeetingInstanceQueryModel> GetInstanceForMeetingByRecurrenceIdAsync(long recurrenceId, CancellationToken cancellationToken)
    {
      return repository.GetMeetingInstanceByRecurrence(recurrenceId, cancellationToken);
    }

  }
}
