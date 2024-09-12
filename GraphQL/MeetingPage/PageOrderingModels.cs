using HotChocolate.Types;

namespace RadialReview.GraphQL.Models.Mutations
{
  public class EditMeetingPageOrder
  {

    #region Properties

    public long MeetingPageId { get; set; }

    public int OldIndex { get; set; }

    public int NewIndex { get; set; }

    #endregion

  }
}
