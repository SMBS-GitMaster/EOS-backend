using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{

  public partial interface IDataContext
  {

    Task<WorkspaceQueryModel> GetWorkspaceAsync(long id, CancellationToken cancellationToken);

    Task<IQueryable<WorkspaceQueryModel>> GetWorkspacesAsync(long userId, CancellationToken cancellationToken);

    Task<IQueryable<WorkspaceQueryModel>> GetWorkspacesForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken);

    Task<WorkspaceQueryModel> GetWorkspaceForMeetingAsync(long recurrenceId, CancellationToken cancellationToken);

    Task<WorkspaceTileQueryModel> GetMeetingWorkspaceTile(long id, CancellationToken cancellationToken);

    Task<WorkspaceTileQueryModel> GetPersonalWorkspaceTile(long id, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public async Task<WorkspaceQueryModel> GetWorkspaceAsync(long id, CancellationToken cancellationToken) {
      return repository.GetWorkspace(id, cancellationToken);
    }

    public async Task<IQueryable<WorkspaceQueryModel>> GetWorkspacesAsync(long userId, CancellationToken cancellationToken)
    {
      return repository.GetWorkspaces(userId, cancellationToken);
    }

    public async Task<IQueryable<WorkspaceQueryModel>> GetWorkspacesForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      return repository.GetWorkspacesForUsers(userIds, cancellationToken);
    }

    public async Task<WorkspaceQueryModel> GetWorkspaceForMeetingAsync(long recurrenceId, CancellationToken cancellationToken)
    {
      return repository.GetWorkspaceForMeeting(recurrenceId, cancellationToken);
    }

    public async Task<WorkspaceTileQueryModel> GetMeetingWorkspaceTile(long id, CancellationToken cancellationToken)
    {
      return repository.GetMeetingWorkspaceTile(id, cancellationToken);
    }

    public async Task<WorkspaceTileQueryModel> GetPersonalWorkspaceTile(long id, CancellationToken cancellationToken)
    {
      return repository.GetPersonalWorkspaceTile(id, cancellationToken);
    }

  }
}
