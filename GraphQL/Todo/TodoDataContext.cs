using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    Task<IQueryable<TodoQueryModel>> GetTodosForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken, bool onlyActives = false);

    Task<IQueryable<TodoQueryModel>> GetTodosForUserAsync(long id, CancellationToken cancellationToken);

    Task<TodoQueryModel> GetTodoByIdAsync(long id, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public async Task<IQueryable<TodoQueryModel>> GetTodosForMeetingsAsync(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken, bool onlyActives = false)
    {
      return await repository.GetTodosForMeetings(recurrenceIds, cancellationToken, onlyActives);
    }

    public async Task<IQueryable<TodoQueryModel>> GetTodosForUserAsync(long userId, CancellationToken cancellationToken)
    {
      return repository.GetTodosForUser(userId, cancellationToken);
    }

    public async Task<TodoQueryModel> GetTodoByIdAsync(long id, CancellationToken cancellationToken)
    {
      return repository.GetTodoById(id, cancellationToken);
    }

  }

}
