using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using RadialReview.Core.Utilities.Types;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;
using static RadialReview.Accessors.IssuesAccessor;

namespace RadialReview.Models.Issues {
  public class IssueModel : BaseModel, ILongIdentifiable, IDeletable, ITodo {
    public virtual long Id { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual string Message { get; set; }
    public virtual string Description { get; set; }
    public virtual string PadId { get; set; }
    public virtual long CreatedById { get; set; }
    public virtual UserOrganizationModel CreatedBy { get; set; }
    public virtual long? CreatedDuringMeetingId { get; set; }
    public virtual L10Meeting CreatedDuringMeeting { get; set; }
    public virtual List<L10Recurrence> _MeetingRecurrences { get; set; }
    public virtual long? _Order { get; set; }
    public virtual int _Priority { get; set; }

    public virtual String ForModel { get; set; }
    public virtual long ForModelId { get; set; }
    public virtual long OrganizationId { get; set; }
    public virtual OrganizationModel Organization { get; set; }
    public virtual int _Rank { get; set; }

    public virtual string ContextNodeType { get; set; }

    public virtual string ContextNodeTitle { get; set; }

    public virtual async Task<string> GetTodoMessage() {
      return "";
    }

    public virtual async Task<string> GetTodoDetails(INotesProvider notesProvider) {
      var padDetails = await notesProvider.GetTextForPad(PadId);
      var header = "RESOLVE ISSUE: " + Message;
      if (!String.IsNullOrWhiteSpace(padDetails)) {
        header += "\n\n" + padDetails;
      }
      return header;
    }


    public IssueModel() {
      CreateTime = DateTime.UtcNow;
      _Order = -CreateTime.Ticks;
    }

    public class IssueMap : BaseModelClassMap<IssueModel> {
      public IssueMap() {
        Id(x => x.Id);
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);

        Map(x => x.ForModel);
        Map(x => x.ForModelId);

        Map(x => x.PadId);


        Map(x => x.Message).Length(10000);
        Map(x => x.Description).Length(10000);
        Map(x => x.CreatedById).Column("CreatedById");
        References(x => x.CreatedBy).Column("CreatedById").LazyLoad().ReadOnly();
        Map(x => x.CreatedDuringMeetingId).Column("CreatedDuringMeetingId");
        References(x => x.CreatedDuringMeeting).Column("CreatedDuringMeetingId").Nullable().LazyLoad().ReadOnly();

        Map(x => x.OrganizationId).Column("OrganizationId");
        References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();

        Map(x => x.ContextNodeTitle);
        Map(x => x.ContextNodeType);
      }
    }


    public class IssueModel_Recurrence : BaseModel, ILongIdentifiable, IDeletable {
      public virtual long Id { get; set; }
      public virtual int Priority { get; set; }
      public virtual DateTime LastUpdate_Priority { get; set; }
      public virtual DateTime LastUpdate_Stars { get; set; }
      public virtual DateTime CreateTime { get; set; }
      public virtual DateTime? DeleteTime { get; set; }
      public virtual DateTime? CloseTime { get; set; }


      public virtual UserOrganizationModel Owner { get; set; }
      public virtual UserOrganizationModel CreatedBy { get; set; }
      public virtual IssueModel_Recurrence CopiedFrom { get; set; }
      public virtual IssueModel Issue { get; set; }
      public virtual L10Recurrence Recurrence { get; set; }
      public virtual List<IssueModel.IssueModel_Recurrence> _ChildIssues { get; set; }
      public virtual IssueModel_Recurrence ParentRecurrenceIssue { get; set; }
      public virtual long? Ordering { get; set; }

      public virtual string FromWhere { get; set; }

      public virtual string _MovedToMeetingName { get; set; }

      public virtual long _MovedToIssueId { get; set; }

      // This field is only for V3
      public virtual IssueModel_Recurrence _SentToIssue { get; set; }

      public virtual int Rank { get; set; }

      public virtual int Stars { get; set; }

      public virtual bool AwaitingSolve { get; set; }
      public virtual bool MarkedForClose { get; set; }
      public virtual bool HasAudio { get; set; }

      public virtual bool AddToDepartmentPlan { get; set; }

      public virtual MergedIssueData MergedIssueData { get; set; }

      public virtual IssueCompartment? IssueCompartment { get; set; }


      public IssueModel_Recurrence() {
        CreateTime = DateTime.UtcNow;
        Ordering = -CreateTime.Ticks;
      }

      public class IssueModel_RecurrenceMap : BaseModelClassMap<IssueModel_Recurrence> {
        public IssueModel_RecurrenceMap() {
          Id(x => x.Id);
          Map(x => x.CreateTime);
          Map(x => x.DeleteTime);
          Map(x => x.LastUpdate_Priority);
          Map(x => x.CloseTime);
          Map(x => x.Priority);
          Map(x => x.Ordering);
          Map(x => x.Rank);
          Map(x => x.Stars);
          Map(x => x.LastUpdate_Stars);
          Map(x => x.HasAudio);

          Map(x => x.AwaitingSolve);
          Map(x => x.AddToDepartmentPlan);
          Map(x => x.IssueCompartment);
          Map(x => x.MergedIssueData)
           .CustomType<NHJsonType<MergedIssueData>>();

          Map(x => x.MarkedForClose);

          Map(x => x.FromWhere);

          References(x => x.CreatedBy).Column("CreatedById");
          References(x => x.CopiedFrom).Column("CopiedFromId").Nullable();
          References(x => x.Owner).Column("OwnerId").Nullable();

          References(x => x.Issue).Column("IssueId");
          References(x => x.Recurrence).Column("RecurrenceId");

          References(x => x.ParentRecurrenceIssue).Column("ParentRecurrenceIssueId").Nullable();
        }
      }
    }
  }
}
