using NHibernate;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks
{
  public interface IWorkspaceHook : IHook
  {

    Task UpdateMeetingWorkspace(ISession s, UserOrganizationModel caller, WorkspaceQueryModel workspace, long recurrenceId);

    Task CreateWorkspace(ISession s, UserOrganizationModel caller, WorkspaceQueryModel workspace, long ownerId);

    Task DeleteWorkspace(ISession s, UserOrganizationModel caller, WorkspaceQueryModel workspace, long ownerId);

    Task UpdateWorkspace(ISession s, UserOrganizationModel caller, WorkspaceQueryModel workspace);

  }
}
