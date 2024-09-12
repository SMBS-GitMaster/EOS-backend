using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Models.Mutations
{

  public class MeetingAddAttendeeModel
  {
    public long UserId { get; set; }
    public long MeetingId { get; set; }
  }


  public class MeetingEditAttendeeModel
  {
    public long MeetingAttendee { get; set; }
    public long MeetingId { get; set; }
    public bool? IsPresent { get; set; }
    public bool? HasSubmittedVotes { get; set; }
    public bool? IsUsingV3 { get; set; }
    public MeetingEditAttendeePermissions? permissions { get; set; }
  }

  public class MeetingEditAttendeePermissions
  {
    public bool? View { get; set; }

    public bool? Edit { get; set; }

    public bool? Admin { get; set; }
  }

  public class MeetingRemoveAttendeeModel
  {
    public long UserId { get; set; }
    public long MeetingId { get; set; }
  }

  public class MeetingAttendeeIsPresentModel
  {
    public long MeetingAttendee { get; set; }
    public long MeetingId { get; set; }
    public bool? IsPresent { get; set; }
  }
}