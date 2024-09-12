using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;

namespace RadialReview.GraphQL.Models.Mutations
{
  public class EditMeetingInstanceAttendeeModel
  {
    public long UserId { get; set; }
    public long MeetingInstanceId { get; set; }
    [DefaultValue(null)] public Optional<decimal?> Rating { get; set; }
    [DefaultValue(null)] public string NotesText { get; set; }
  }
}
