using RadialReview.Models.Interfaces;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Models.L10;
using FluentNHibernate.Mapping;

namespace RadialReview.Core.Models.L10
{
  public class L10MeetingNote : BaseModel, ILongIdentifiable, IDeletable
  {
    public virtual long Id { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual string NotesId { get; set; }
    public virtual long MeetingId { get; set; }
    public virtual L10Note L10Note { get; set; }
  }

  public class L10MeetingNoteMap : BaseModelClassMap<L10MeetingNote>
  {
    public L10MeetingNoteMap()
    {
      Id(x => x.Id);
      Map(x => x.DeleteTime);
      Map(x => x.MeetingId);
      Map(x => x.NotesId);
      References(x => x.L10Note).Column("NotesId").PropertyRef("PadId").ReadOnly();
    }
  }

}
