using RadialReview.Models.L10.VM;
using System;
using System.Collections.Generic;

namespace RadialReview.GraphQL.Models
{
  public class MeetingInstanceQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double? DateCreated { get; set; }
    public double? DateLastModified { get; set; }

    #endregion

    #region Properties

    public decimal AverageMeetingRating { get; set; }

    public decimal TodosCompletedPercentage { get; set; }

    public int IssuesSolvedCount { get; set; }

    public double? MeetingDurationInSeconds { get; set; }

    public double? MeetingStartTime { get; set; }

    public long LeaderId { get; set; }

    public bool IsPaused { get; set; }

    public double? MeetingConcludedTime { get; set; }

    public bool IssueVotingHasEnded { get; set; }
    public double? TangentAlertTimestamp {get; set; }
    public TimerByPageModel TimerByPage { get; set; }
    public long RecurrenceId { get; set; }
    public List<MeetingNoteQueryModel> Notes { get; set; }
    public List<string> SelectedNotes { get; set; }
    public bool IsV1PreviewMeeting { get; set; }
    public string IssueVotingType { get; set; }
    #endregion

    public static class Collections
    {
      public enum MeetingNote2
      {
        Notes
      }

      public enum MeetingAttendee2
      {
        MeetingAttendee
      }

      public enum MeetingInstanceAttendee
      {
        MeetingInstanceAttendees
      }
    }

    public static class Associations
    {
      public enum User10
      {
        User
      }
    }

  }
}