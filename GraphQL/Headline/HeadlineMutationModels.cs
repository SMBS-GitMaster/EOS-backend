using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models.Mutations
{
  public class HeadlineCreateModel
  {
    public string Title { get; set; }
    public long Assignee { get; set; }
    public long[] Meetings { get; set; }

    [DefaultValue(false)] public bool? Archived { get; set; }

    [DefaultValue(null)] public double? ArchivedTimestamp { get; set; }
    [DefaultValue(null)] public string NotesId { get; set; }
  }
  public class HeadlineEditModel
  {
    public long HeadlineId { get; set; }

    [DefaultValue(null)] public string Title { get; set; }
    [DefaultValue(null)] public long? Assignee { get; set; }
    [DefaultValue(null)] public long[] Meetings { get; set; }
    [DefaultValue(null)] public bool? Archived { get; set; }
    [DefaultValue(null)] public double? ArchivedTimestamp { get; set; }

    [DefaultValue(null)] public string NotesId { get; set; }
  }

  public class CopyHeadlineToMeetingsModel
  {
    public long HeadlineToCopyId { set; get; }
    public long[] MeetingIds { get; set; }
    public string NotesText { set; get; }
  }

  public class NoteMeeting
  {
    public long MeetingId { get; set; }
    public string NotePadId { get; set; }
  }
}
