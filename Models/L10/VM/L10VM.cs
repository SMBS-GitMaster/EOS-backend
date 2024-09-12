using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using static RadialReview.Models.L10.L10Recurrence;

namespace RadialReview.Models.L10.VM {
  public class L10VM {

    public TinyRecurrence Recurrence { get; set; }
    public bool? IsAttendee { get { return Recurrence.IsAttendee; } }

    public L10VM(TinyRecurrence recurrence) {
      Recurrence = recurrence;
    }
  }

  public class TinyRecurrence : BaseModel {
    public long Id { get; set; }
    public string Name { get; set; }
    public long? MeetingInProgress { get; set; }
    public bool IsAttendee { get; set; }
    public List<TinyUser> _DefaultAttendees { get; set; }
    public List<L10Recurrence_Attendee> L10Recurrence_Attendees { get; set; }
    public DateTime? StarDate { get; set; }
    public FavoriteModel Favorite { get; set; }

    public bool IsFavorited { get { return (Favorite!=null && Favorite.DeleteTime==null) || StarDate !=null; } }

    public MeetingType MeetingType { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? DeleteTime { get; set; }
    public List<L10Recurrence_Page> _Pages { get; set; }
  }

  public class V3TinyRecurrence {
    public long Id { get; set; }
    public string Name { get; set; }
    public bool CurrentUserCanAdmin { get; set; }
  }
}
