using NHibernate;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks
{
  public interface IWorkspaceNoteHook : IHook
  {

    Task InsertWorkspaceNote(ISession s, UserOrganizationModel caller, WorkspaceNoteQueryModel tile);

    Task UpdateWorkspaceNote(ISession s, UserOrganizationModel caller, WorkspaceNoteQueryModel tile);

  }
}
