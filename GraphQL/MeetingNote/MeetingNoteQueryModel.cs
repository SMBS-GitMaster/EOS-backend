using System;
using System.Collections.Generic;

namespace RadialReview.GraphQL.Models
{
  public class MeetingNoteQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double? DateCreated { get; set; }
    public double DateLastModified { get; set; }
    public double? LastUpdatedClientTimestemp { get; set; }
    public string Type { get { return "meetingNote"; } }

    #endregion

    #region Properties

    public string Title { get; set; }

    public string NotesId { get; set; }

    public bool Archived { get; set; }

    public double? ArchivedTimestamp { get; set; }

    public UserQueryModel Owner { get; set; }

    public long? OwnerId { get; set; }

    #endregion

    public static class Associations 
    {
      public enum User4 
      {
        Owner
      }
    }

  }
}