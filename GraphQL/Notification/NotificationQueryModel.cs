namespace RadialReview.GraphQL.Models
{
  public class NotificationQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }

    #endregion

    #region Properties

    public string Text { get; set; }
    public string NotificationType { get; set; }
    public string ViewState { get; set; }
    public long UserId { get; set; }
    public long MentionerId { get; set; }
    public long MeetingId { get; set; }
    public long TodoId { get; set; }

    // TODO: NOTE: I've added this field to resolve FE errors in setting up subscriptions.  But I don't think this field belongs here.
    public string WeekStart { get; set; } = "NOTE: I've added this field to resolve FE errors in setting up subscriptions.  But I don't think this field belongs here.";

    #endregion

  }

}