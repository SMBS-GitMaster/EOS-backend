using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{

  public partial interface IDataContext
  {

    Task<UserQueryModel> GetUserByIdAsync(long? userId, CancellationToken cancellationToken);

    Task<IQueryable<UserQueryModel>> GetUsersAsync(CancellationToken cancellationToken);
    Task<IQueryable<UserQueryModel>> GetTrackedMetricCreators(List<long> userIds, CancellationToken cancellationToken);

    Task<IQueryable<MeetingMetadataModel>> GetPossibleMeetingsForUser(CancellationToken cancellationToken);

    Task<IQueryable<MeetingLookupModel>> GetMeetingsForUserByAdminStatus(CancellationToken cancellationToken);

    Task<IQueryable<SelectListQueryModel>> GetAdminPermissionsMeetingsLookup(long userId, CancellationToken ct);

    Task<string> GetCoachToolsURL(CancellationToken cancellationToken);

    Task<IQueryable<SelectListQueryModel>> GetEditPermissionsMeetingsLookup(long userId, CancellationToken ct);

    Task<IQueryable<KeyValuePair<long, string>>> GetSupportContactCodeForUsers(IEnumerable<long> userIds, CancellationToken ct);

    Task<OrgSettingsModel> GetOrgSettings(long userId, CancellationToken ct);

    Task<IQueryable<TodoQueryModel>> GetUserTodosAsync(long userId, CancellationToken cancellationToken);

    Task<IQueryable<OrganizationQueryModel>> GetOrganizationsForUser(long userId, CancellationToken cancellationToken);

    Task<IQueryable<GoalQueryModel>> GetGoalsForUser(CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public async Task<UserQueryModel> GetUserByIdAsync(long? userId, CancellationToken cancellationToken)
    {
      return repository.GetUserById(userId, cancellationToken);
    }

    public async Task<IQueryable<UserQueryModel>> GetUsersAsync(CancellationToken cancellationToken)
    {
      return repository.GetUsers(cancellationToken);
    }

    public async Task<IQueryable<UserQueryModel>> GetTrackedMetricCreators(List<long> ids, CancellationToken cancellationToken)
    {
      return repository.GetTrackedMetricCreators(ids, cancellationToken);
    }

    public async Task<IQueryable<MeetingMetadataModel>> GetPossibleMeetingsForUser(CancellationToken cancellationToken)
    {
      return repository.GetPossibleMeetingsForUser(cancellationToken);
    }

    public async Task<IQueryable<MeetingLookupModel>> GetMeetingsForUserByAdminStatus(CancellationToken cancellationToken)
    {
      return repository.GetMeetingsForUserByAdminStatus(cancellationToken);
    }

    public Task<IQueryable<SelectListQueryModel>> GetAdminPermissionsMeetingsLookup(long userId, CancellationToken ct)
    {
      return Task.FromResult(repository.GetAdminPermissionsMeetingsLookup(userId, ct));
    }

    public Task<string> GetCoachToolsURL(CancellationToken cancellationToken)
    {
      return repository.GetCoachToolsURL(cancellationToken);
    }

    public Task<IQueryable<SelectListQueryModel>> GetEditPermissionsMeetingsLookup(long userId, CancellationToken ct)
    {
      return Task.FromResult(repository.GetEditPermissionsMeetingsLookup(userId, ct));
    }

    public Task<IQueryable<KeyValuePair<long, string>>> GetSupportContactCodeForUsers(IEnumerable<long> userIds, CancellationToken ct)
    {
      return Task.FromResult(repository.GetSupportContactCodeForUsers(userIds, ct));
    }

    public Task<OrgSettingsModel> GetOrgSettings(long userId, CancellationToken ct)
    {
      return Task.FromResult(repository.GetOrgSettings(userId, ct));
    }

    public Task<IQueryable<TodoQueryModel>> GetUserTodosAsync(long userId, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetUserTodos(userId, cancellationToken));
    }

    public Task<IQueryable<OrganizationQueryModel>> GetOrganizationsForUser(long userId, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetOrganizationsForUser(userId, cancellationToken));
    }

    public Task<IQueryable<GoalQueryModel>> GetGoalsForUser(CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetGoalsForUser(cancellationToken));
    }

  }
}