using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;
using NHibernate.Type;

namespace RadialReview.Core.Models.L10
{
  public enum SendEmailSummaryTo
  {
    NONE,
    ALL_ATTENDEES,
    ALL_ATTENDEES_RATED_MEETING
  }

  public enum FeedbackStyle
  {
    INDIVIDUAL,
    ALL_PARTICIPANTS
  }

  public class MeetingConclusionModel : BaseModel, ILongIdentifiable, IDeletable
  {
    public virtual long Id { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual SendEmailSummaryTo? SendEmailSummaryTo { get; set; }
    public virtual bool? IncludeMeetingNotesInEmailSummary { get; set; }
    public virtual bool? ArchiveCompletedTodos { get; set; }
    public virtual bool? ArchiveHeadlines { get; set; }
    public virtual bool? ArchiveCompletedIssues { get; set; }
    public virtual bool? DisplayMeetingRatings { get; set; }
    public virtual FeedbackStyle? FeedbackStyle { get; set; }
    public virtual long L10RecurrenceId { get; set; }
    public virtual L10Recurrence L10Recurrence { get; set; }
    public MeetingConclusionModel()
    {
      CreateTime = DateTime.UtcNow;
    }
    public class ActionsConcludeRecurrenceMap : BaseModelClassMap<MeetingConclusionModel>
    {
      public ActionsConcludeRecurrenceMap()
      {
        Id(x => x.Id);
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);
        Map(x => x.SendEmailSummaryTo).CustomType<EnumSendEmailSummaryToStringType>();
        Map(x => x.IncludeMeetingNotesInEmailSummary);
        Map(x => x.ArchiveCompletedTodos);
        Map(x => x.ArchiveHeadlines);
        Map(x => x.ArchiveCompletedIssues);
        Map(x => x.DisplayMeetingRatings);
        Map(x => x.FeedbackStyle).CustomType<EnumFeedbackStyleToStringType>(); ;
        Map(x => x.L10RecurrenceId).Column("L10RecurrenceId");
        References(x => x.L10Recurrence).Column("L10RecurrenceId").LazyLoad().ReadOnly();
      }
    }
  }

  public class EnumSendEmailSummaryToStringType : EnumStringType
  {
    public EnumSendEmailSummaryToStringType() : base(typeof(SendEmailSummaryTo)) { }
  }

  public class EnumFeedbackStyleToStringType : EnumStringType
  {
    public EnumFeedbackStyleToStringType() : base(typeof(FeedbackStyle)) { }
  }
}
