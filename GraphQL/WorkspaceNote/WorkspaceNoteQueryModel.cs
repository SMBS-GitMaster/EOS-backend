namespace RadialReview.GraphQL.Models
{
  public class WorkspaceNoteQueryModel
  {

    #region Properties

    public long Id { get; set; }

    public int Version { get; set; }

    public string LastUpdatedBy { get; set; }

    public double? DateCreated { get; set; }

    public double? DateLastModified { get; set; }

    public bool Archived { get; set; }

    public double? ArchivedTimestamp { get; set; }

    public string Title { get; set; }

    public string NotesId { get; set; }

    public long WorkspaceId { get; set; }

    #endregion

  }
}