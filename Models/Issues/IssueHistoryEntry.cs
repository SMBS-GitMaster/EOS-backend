using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;

namespace RadialReview.Models.Issues {
  public class IssueHistoryEntry : ILongIdentifiable {
    public virtual long Id { get; set; }
    public virtual DateTime CreateTime { get; set; }

    public virtual IssueHistoryEventType EventType { get; set; }
    public virtual DateTime? ValidFrom { get; set; }
    public virtual DateTime? ValidUntil { get; set; }

    /* TODO: NOTE: Now that IssueSentTo has been created the following four fields can be deleted
    ** and replaced by a reference to IssueSentTo.
    ** This has to be done with care as the table exists in the PROD database.
    */
    public virtual Models.L10.L10Recurrence Meeting { get; set; }
    public virtual IssueModel Issue { get; set; }

    public virtual long MeetingId { get; set; }
    public virtual long IssueId { get; set; }
  }

  public enum IssueHistoryEventType {
    Created,
    Moved,
    Solved,
    InformationPrivileged
  }

  public class IssueHistoryEntryMap : ClassMap<IssueHistoryEntry> {
    public IssueHistoryEntryMap() {
      Id(x => x.Id);
      Map(x => x.CreateTime);

      Map(x => x.EventType);
      Map(x => x.ValidFrom);
      Map(x => x.ValidUntil);

      References(x => x.Meeting).ReadOnly().LazyLoad().Column("RecurrenceId");
      References(x => x.Issue).ReadOnly().LazyLoad().Column("IssueId");

      Map(x => x.MeetingId).Column("RecurrenceId");
      Map(x => x.IssueId).Column("IssueId");
    }
  }
}