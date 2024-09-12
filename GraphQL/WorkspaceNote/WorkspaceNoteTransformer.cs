using RadialReview.GraphQL.Models;
using RadialReview.Models;

namespace RadialReview.Repositories
{
  public static class PersonalNoteTransformer
  {

    public static WorkspaceNoteQueryModel Transform(this PersonalNote model)
    {
      if (model == null) return null;
      return new WorkspaceNoteQueryModel()
      {
        Id = model.Id,
        Version = model.Version,
        DateCreated = model.DateCreated.ToUnixTimeStamp(),
        DateLastModified = model.DateLastModified.ToUnixTimeStamp(),
        LastUpdatedBy = model.LastUpdatedBy,
        WorkspaceId = model.WorkspaceId,
        NotesId = model.PadId,
        Title = model.Title,
        Archived = model.DeleteTime != null,
        ArchivedTimestamp = model.DeleteTime.ToUnixTimeStamp()
      };
    }

  }
}
