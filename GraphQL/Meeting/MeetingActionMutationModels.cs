using HotChocolate.Types;
using RadialReview.Models.L10.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models.Mutations {

  public class StartMeetingModel {
    public long MeetingId { get; set; }
    public double MeetingStartTime { get; set; }
    public string Mode {  get; set; }
  }

  public class RestartMeetingModel {
    public long MeetingId { get; set; }
    public long TimeRestarted { get; set; }
  }

  public class ConcludeMeetingModel {
    public long MeetingId { get; set; }
    public long EndTime { get; set; }
    public bool ArchiveCompletedTodos { get; set; }
    public bool ArchiveHeadlines { get; set; }
    public string SendEmailSummaryTo { get; set; }
    public bool IncludeMeetingNotesInEmailSummary { get; set; }
    public List<long> SelectedNotes { get; set; }
    public bool? DisplayMeetingRatings { get; set; } 
    public string FeedbackStyle { get; set; }

  }

  public class SetMeetingPageModel {
    public long MeetingId { get; set; }
    public long NewPageId { get; set; }
    public double MeetingPageStartTime { get; set; }
  }

  public class SetCurrentMeetingPageModel {
    public long MeetingId { get; set; }
    public long NewPageId { get; set; }
    public long CurrentPageId { get; set; }
    public double MeetingPageStartTime { get; set; }
  }

  public class RateMeetingModel {
    public long MeetingId { get; set; }
    public int Rating { get; set; }
    public string Notes { get; set; }
  }

  public class CreateMeetingPageModel
  {
    public string PageType { get; set; }
    public string PageName { get; set; }
    public long RecurrenceId { get; set; }
    public decimal ExpectedDurationS { get; set; }
  }
}
