namespace RadialReview.Core.Utilities.PermisionMessages
{
  public class PermissionMessages
  {
    public enum MeetingPermissionMessage
    {
      EditPermissionRequired,
      AdminPermissionRequired
    }

    public static string GetMessage(MeetingPermissionMessage messageType)
    {
      switch (messageType)
      {
        case MeetingPermissionMessage.EditPermissionRequired:
          return "You must have edit permissions in this meeting to add this item";
        case MeetingPermissionMessage.AdminPermissionRequired:
          return "You must have admin permissions in this meeting to add this item";
        default:
          return "requires permissions to perform this action";
      }
    }
  }
}
