using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    Task<IQueryable<MeetingNoteQueryModel>> GetNotesForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public Task<IQueryable<MeetingNoteQueryModel>> GetNotesForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetNotesForMeetings(recurrenceIds, cancellationToken));
    }

  }
}
