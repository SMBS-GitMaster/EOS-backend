// Source: L10Recurrence_Attendee table

using RadialReview.Core.GraphQL.Enumerations;
using System;

namespace RadialReview.GraphQL.Models {

  public class MeetingAttendeeQueryModel {

    public MeetingAttendeeQueryModel() { }

    public MeetingAttendeeQueryModel(long recurId) {
      if (recurId == 0)
        throw new Exception("MeetingId cannot be zero");
      MeetingId = recurId;
    }

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }
    public double LastUpdatedClientTimestamp { get; set; }

    #endregion

    #region Properties

    public string Avatar { get; set; }
    public gqlUserAvatarColor? UserAvatarColor { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string FullName { get; set; }

    public bool IsLeader { get; set; }

    public bool IsPresent { get; set; }

    public long MeetingId { get; set; }

    public bool? IsUsingV3 { get; set; }

    public bool HasSubmittedVotes { get; set; }

    public UserQueryModel User { get; set; }

    #endregion

    public static class Associations {
      public enum User1 {
        User
      }

      public enum MeetingInstanceAttendee3 {
        MeetingInstanceAttendee
      }
    }

    public static class Collections {
      public enum MeetingInstanceAttendee2 {
        MeetingInstanceAttendee
      }
    }

  }

  public class MeetingAttendeeQueryModelLookup {
    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }
    public double LastUpdatedClientTimestamp { get; set; }
    public gqlUserAvatarColor? UserAvatarColor { get; set; }


    #endregion
    public MeetingAttendeeQueryModelLookup() { }

    public MeetingAttendeeQueryModelLookup(long recurId) {
      if (recurId == 0)
        throw new Exception("MeetingId cannot be zero");
      MeetingId = recurId;
    }
    public string Avatar { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName { get; set; }
    public long MeetingId { get; set; }

    public UserQueryModel User { get; set; }
  }
}