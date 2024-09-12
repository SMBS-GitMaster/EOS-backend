using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    Task<IQueryable<HeadlineQueryModel>> GetHeadlinesForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken);

    Task<IQueryable<HeadlineQueryModel>> GetHeadlinesForUserAsync(long userId, CancellationToken cancellationToken);

    Task<HeadlineQueryModel> GetHeadlineByIdAsync(long id, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public async Task<IQueryable<HeadlineQueryModel>> GetHeadlinesForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken)
    {
      return await repository.GetHeadlinesForMeetings(recurrenceIds, cancellationToken);
    }

    public async Task<IQueryable<HeadlineQueryModel>> GetHeadlinesForUserAsync(long userId, CancellationToken cancellationToken)
    {
      return repository.GetHeadlinesForUser(userId, cancellationToken);
    }

    public async Task<HeadlineQueryModel> GetHeadlineByIdAsync(long id, CancellationToken cancellationToken)
    {
      return repository.GetHeadlineById(id, cancellationToken);
    }

  }
}
