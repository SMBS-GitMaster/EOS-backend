namespace RadialReview.Core.GraphQL.Models.Mutations
{

  public class CreateWorkspaceNoteModel
  {

    public long WorkspaceId { get; set; }

    public string Title { get; set; }

  }

  public class EditWorkspaceNoteModel
  {

    public long WorkspaceNoteId { get; set; }

    public string? Title { get; set; }

    public bool? Archived { get; set; }

  }
}