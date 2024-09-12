
using HotChocolate.Types;

namespace RadialReview.GraphQL.Models.Mutations
{

  public class NoteCreateModel
  {
    #region Properties

    public string Title { get; set; }

    public long MeetingId { get; set; }

    [DefaultValue(null)] public string NotesId { get; set; }

    #endregion
  }

  public class EditNoteModel
  {
    #region Properties

    public long MeetingNoteId { get; set; }

    public long MeetingId { get; set; }

    [DefaultValue(null)] public string NotesId { get; set; }

    [DefaultValue(null)] public string Title { get; set; }

    [DefaultValue(null)] public bool? Archived { get; set; }

    #endregion
  }
}