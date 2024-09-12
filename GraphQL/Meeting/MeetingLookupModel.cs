using HotChocolate.Data;
using RadialReview.GraphQL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models
{
  public class MeetingLookupModel
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public bool IsCurrentUserAdmin { get; set; }

    public IQueryable<MeetingPageQueryModel> MeetingPages { get; set; }
    public IQueryable<MeetingAttendeeQueryModel> Attendees { get; set; }
  }
}
