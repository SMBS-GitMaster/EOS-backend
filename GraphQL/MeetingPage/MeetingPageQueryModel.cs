using RadialReview.Core.GraphQL.Enumerations;

namespace RadialReview.GraphQL.Models
{
  public class MeetingPageQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }

    #endregion

    #region Properties
    public double? DateDeleted { get; set; }

    public long MeetingId { get; set; }
    public gqlPageType PageType { get; set; }

    public string NoteboxPadId { get; set; }

    public decimal Minutes { get; set; }

    public decimal ExpectedDurationS { get; set; }

    public string PageName { get; set; }
    public string Subheading { get; set; }

    public int Order { get; set; }

    public TimerModel Timer { get; set; }

    public string ExternalPageUrl { get; set; }

    public MeetingPageTitleModel Title { get; set; }

    public CheckInModel CheckIn { get; set; }

    #endregion

    public static class Associations 
    {
      public enum User8 
      {
        User
      }
    }

  }

  public class MeetingPageTitleModel
  {

    #region Properties

    public string Text { get; set; }

    public long NoteId { get; set; }

    public string NoteText { get; set; }

    #endregion

  }

}