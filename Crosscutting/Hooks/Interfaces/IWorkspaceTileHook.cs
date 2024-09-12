using NHibernate;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks
{
  public interface IWorkspaceTileHook : IHook
  {

    Task InsertWorkspaceTile(ISession s, UserOrganizationModel caller, WorkspaceTileQueryModel tile, WorkspaceQueryModel workspace);

    Task RemoveWorkspaceTile(ISession s, UserOrganizationModel caller, WorkspaceTileQueryModel tile, long workspaceId);

    Task RemoveWorkspaceTile(ISession s, UserOrganizationModel caller, WorkspaceTileQueryModel tile, WorkspaceQueryModel workspace);

    Task UpdateWorkspaceTile(ISession s, UserOrganizationModel caller, WorkspaceTileQueryModel tile);

  }
}
