using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Crosscutting.Hooks;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    #region Queries

    List<WorkspaceNoteQueryModel> GetWorkspacePersonalNotes(long workspaceId, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<IdModel> CreateWorkspaceNote(CreateWorkspaceNoteModel input);

    Task<IdModel> EditWorkspaceNote(EditWorkspaceNoteModel input);

    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public List<WorkspaceNoteQueryModel> GetWorkspacePersonalNotes(long workspaceId, CancellationToken cancellationToken)
    {
      return PersonalNoteAccessor.GetPersonalNotesForWorkspace(caller, workspaceId).Select(_ => _.Transform()).ToList();
    }

    #endregion

    #region Mutations

    public async Task<IdModel> CreateWorkspaceNote(CreateWorkspaceNoteModel input)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          PersonalNote result = await PersonalNoteAccessor.CreatePersonalNote(caller, input.WorkspaceId, input.Title);

          await HooksRegistry.Each<IWorkspaceNoteHook>((ses, x) => x.UpdateWorkspaceNote(ses, caller, result.Transform()));
          await HooksRegistry.Each<IWorkspaceNoteHook>((ses, x) => x.InsertWorkspaceNote(ses, caller, result.Transform()));

          return new IdModel(result.Id);

        }
      }

    }

    public async Task<IdModel> EditWorkspaceNote(EditWorkspaceNoteModel input)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          PersonalNote result = await PersonalNoteAccessor.EditPersonalNote(caller, input.WorkspaceNoteId, input.Title, input.Archived);

          //await HooksRegistry.Each<IWorkspaceNoteHook>((ses, x) => x.UpdateWorkspaceNote(ses, caller, result.Transform()));

          return new IdModel(input.WorkspaceNoteId);
        }
      }
    }

    #endregion

  }
}
