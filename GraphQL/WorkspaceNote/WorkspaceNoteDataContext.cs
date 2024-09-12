using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{

  public partial interface IDataContext
  {

    #region Queries

    Task<List<WorkspaceNoteQueryModel>> GetWorkspacePersonalNotes(long workspaceId, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<IdModel> CreatePersonalNote(CreateWorkspaceNoteModel input);

    #endregion

  }

  public partial class DataContext : IDataContext
  {

    #region Queries

    public Task<List<WorkspaceNoteQueryModel>> GetWorkspacePersonalNotes(long workspaceId, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetWorkspacePersonalNotes(workspaceId, cancellationToken));
    }

    #endregion

    #region Mutations

    public async Task<IdModel> CreatePersonalNote(CreateWorkspaceNoteModel input)
    {
      return await repository.CreateWorkspaceNote(input);
    }

    #endregion

  }

}
