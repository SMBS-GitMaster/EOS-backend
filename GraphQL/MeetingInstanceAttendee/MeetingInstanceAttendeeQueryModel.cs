namespace RadialReview.GraphQL.Models
{
  public class MeetingInstanceAttendeeQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }
    public string Type { get { return "meetingInstanceAttendee"; } }

    #endregion

    #region Properties

    public decimal? Rating { get; set; }
    public string? NotesText { get; set; }
    public bool HasVotedForIssues { get; set; }
    public MeetingAttendeeQueryModel Attendee { get; set; }
    public UserQueryModel User { get; set; }

    #endregion

    public static class Associations 
    {
      public enum User12
      {
        User
      }

      public enum MeetingAttendee4
      {
        Attendee
      }
    }

  }
}