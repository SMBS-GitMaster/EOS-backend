using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;

namespace RadialReview.Models.Issues {

  public class IssueSentToModel : ILongIdentifiable {
    public virtual long Id { get; set; }
    public virtual DateTime CreateTime { get; set; }

    public virtual bool Archived { get; set; }
    public virtual DateTime? ArchivedTimestamp { get; set; }

    public virtual Models.L10.L10Recurrence Meeting { get; set; }
    public virtual IssueModel Issue { get; set; }
    public virtual long MeetingId {get; set;}

    public virtual long IssueId {get; set;}
  }

  public class IssueSentToModelMap : ClassMap<IssueSentToModel> {
    public IssueSentToModelMap() {
      Id(x => x.Id);
      Map(x => x.CreateTime);

      Map(x => x.Archived);
      Map(x => x.ArchivedTimestamp);

      References(x => x.Meeting).ReadOnly().LazyLoad().Column("RecurrenceId");
      References(x => x.Issue).ReadOnly().LazyLoad().Column("IssueId");

      Map(x => x.MeetingId).Column("RecurrenceId");
      Map(x => x.IssueId).Column("IssueId");
    }
  }
}