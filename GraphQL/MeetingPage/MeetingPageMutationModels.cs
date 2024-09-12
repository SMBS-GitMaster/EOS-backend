using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.Core.GraphQL.Models.Mutations
{

  public class EditMeetingPageModel
  {
    public long MeetingPageId { get; set; }
    [DefaultValue(null)] public string PageType { get; set; }
    [DefaultValue(null)] public string PageName { get; set; }
    [DefaultValue(null)] public string Subheading { get; set; }
    [DefaultValue(null)] public double? ExpectedDurationS { get; set; }
    [DefaultValue(null)] public EditPageTimerModel timer { get; set; }
    [DefaultValue(null)] public string ExternalPageUrl { get; set; }
    [DefaultValue(null)] public CheckInModel CheckIn { get; set; }

  }

  public class EditPageTimerModel
  {
    public double? TimeLastStarted { get; set; }

    public double? TimePreviouslySpentS { get; set; }

    public double? TimeLastPaused { get; set; }

    public double? TimeSpentPausedS { get; set; }
  }

  public class RemoveMeetingPageModel
  {
    public long RecurrenceId { get; set; }

    public long MeetingPageId { get; set; }
  }
}
