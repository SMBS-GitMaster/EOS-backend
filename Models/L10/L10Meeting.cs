using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentNHibernate.Mapping;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.VideoConference;
using Newtonsoft.Json;
using RadialReview.Models.L10.VM;
using RadialReview.Middleware.Services.NotesProvider;
using static RadialReview.Models.L10.L10Recurrence;

namespace RadialReview.Models.L10 {
	public class L10Meeting : ILongIdentifiable, IDeletable {
		public virtual long Id { get; set; }
		public virtual Guid HubId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime? StartTime { get; set; }
		public virtual DateTime? CompleteTime { get; set; }

		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual long L10RecurrenceId { get; set; }
		public virtual L10Recurrence L10Recurrence { get; set; }
		public virtual long MeetingLeaderId { get; set; }
		public virtual UserOrganizationModel MeetingLeader { get; set; }

		public virtual long? SelectedVideoProviderId { get; set; }
		public virtual AbstractVCProvider SelectedVideoProvider { get; set; }

		/// <summary>
		/// Current meetings measurables. Needed in case meeting measurables change throughout time
		/// </summary>
		public virtual IList<L10Meeting_Measurable> _MeetingMeasurables { get; set; }
		public virtual IList<L10Meeting_Measurable> _MeetingConnections { get; set; }
		public virtual IList<L10Meeting_Attendee> _MeetingAttendees { get; set; }
		public virtual IList<L10Meeting_Rock> _MeetingRocks { get; set; }
		public virtual IList<L10Meeting_Log> _MeetingLogs { get; set; }
		public virtual IList<Tuple<string, double>> _MeetingLeaderPageDurations { get; set; }

		public virtual String _MeetingLeaderCurrentPage { get; set; }
		public virtual DateTime? _MeetingLeaderCurrentPageStartTime { get; set; }
		public virtual double? _MeetingLeaderCurrentPageBaseMinutes { get; set; }

		public virtual Ratio TodoCompletion { get; set; }

		public virtual bool Preview { get; set; }
		public virtual Ratio AverageMeetingRating { get; set; }
		public virtual ConcludeSendEmail SendConcludeEmailTo { get; set; }
		public virtual bool? IsRecording { get; set; }
		public virtual bool? HasRecording { get; set; }
		public virtual long BeginWhiteboardDiff { get; set; }
		public virtual long CurrentWhiteboardDiff { get; set; }

        //V3 properties
        public virtual bool IssueVotingHasEnded { get; set; }



    public L10Meeting() {
			_MeetingAttendees = new List<L10Meeting_Attendee>();
			_MeetingMeasurables = new List<L10Meeting_Measurable>();
			_MeetingRocks = new List<L10Meeting_Rock>();
			HubId = Guid.NewGuid();
		}

		public class L10MeetingMap : ClassMap<L10Meeting> {
			public L10MeetingMap() {
				Id(x => x.Id);
				Map(x => x.HubId).Column("HubIdStr");
				Map(x => x.SelectedVideoProviderId).Column("SelectedVideoProviderId");
				References(x => x.SelectedVideoProvider).Column("SelectedVideoProviderId").LazyLoad().Nullable().ReadOnly();

				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.StartTime);
				Map(x => x.CompleteTime);
				Map(x => x.SendConcludeEmailTo).CustomType<ConcludeSendEmail>();
				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();
				Map(x => x.L10RecurrenceId).Column("L10RecurrenceId");
				References(x => x.L10Recurrence).Column("L10RecurrenceId").Not.LazyLoad().ReadOnly();
				Map(x => x.MeetingLeaderId).Column("MeetingLeaderId");
				References(x => x.MeetingLeader).Column("MeetingLeaderId").Not.LazyLoad().ReadOnly();

				Map(x => x.Preview);
				Map(x => x.IsRecording);
				Map(x => x.HasRecording);
				Map(x => x.BeginWhiteboardDiff);
				Map(x => x.CurrentWhiteboardDiff);
                Map(x => x.IssueVotingHasEnded);

				Component(x => x.TodoCompletion).ColumnPrefix("TodoCompletion_");
				Component(x => x.AverageMeetingRating).ColumnPrefix("AvgRating_");

			}
		}

		public class L10Meeting_Rock : ILongIdentifiable, IDeletable, IIssue, ITodo {
			public virtual long Id { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual DateTime? CompleteTime { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual RockState Completion { get; set; }
			public virtual L10Meeting L10Meeting { get; set; }
			public virtual RockModel ForRock { get; set; }
			public virtual L10Recurrence ForRecurrence { get; set; }

      public virtual List<GoalRecurrenceRecord> _recurrenceMilestoneSettings { get; set; }

      public virtual bool VtoRock { get; set; }
      public virtual bool? Archived { get; set; }

			public L10Meeting_Rock() {
				CreateTime = DateTime.UtcNow;
				Completion = RockState.Indeterminate;
			}

			public class L10Meeting_RockMap : ClassMap<L10Meeting_Rock> {
				public L10Meeting_RockMap() {
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					Map(x => x.CompleteTime);
					Map(x => x.CreateTime);
					Map(x => x.Completion);
					Map(x => x.VtoRock);
          Map(x => x.Archived);
					References(x => x.ForRock).Column("RockId");
					References(x => x.L10Meeting).Column("MeetingId");
					References(x => x.ForRecurrence).Column("RecurrenceId");
				}
			}

			public virtual async Task<string> GetTodoMessage()
			{
				return "";
			}

			public virtual async Task<string> GetTodoDetails(INotesProvider notesProvider) {
				var week = L10Meeting.CreateTime.StartOfWeek(DayOfWeek.Sunday).ToString("d");
				var accountable = ForRock.AccountableUser.GetName();

				var footer = "'" + ForRock.Rock + "'\n\n" + "Week: " + week + "\nOwner: " + accountable;
				return footer;
			}

			public virtual async Task<string> GetIssueMessage() {
				return ForRock.Rock;
			}

			public virtual async Task<string> GetIssueDetails(INotesProvider notesProvider) {
				var marked = "";
				switch (Completion) {
					case RockState.AtRisk:
						marked = "\nMarked: 'Off Track'";
						break;
					case RockState.OnTrack:
						marked = "\nMarked: 'On Track'";
						break;
					case RockState.Complete:
						marked = "\nMarked: 'Done'";
						break;
				}

				var week = L10Meeting.CreateTime.StartOfWeek(DayOfWeek.Sunday).ToString("d");
				var accountable = ForRock.AccountableUser.GetName();
				var footer = "Week:" + week + "\nOwner: " + accountable;
				footer += marked;
				try {
					var padd = await notesProvider.GetTextForPad(ForRock.PadId);
					if (!string.IsNullOrWhiteSpace(padd)) {
						footer = padd + "\n" + footer;
					}
				} catch (Exception e) {

				}

				return footer;
			}

		}

		public class L10Meeting_Measurable : IDeletable, ILongIdentifiable {
			public virtual long Id { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual L10Meeting L10Meeting { get; set; }
			public virtual MeasurableModel Measurable { get; set; }
			public virtual int _Ordering { get; set; }
			public virtual bool IsDivider { get; set; }
			public L10Meeting_Measurable() {

			}
			public class L10Meeting_MeasurableMap : ClassMap<L10Meeting_Measurable> {
				public L10Meeting_MeasurableMap() {
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					Map(x => x._Ordering);
					Map(x => x.IsDivider);
					References(x => x.Measurable).Column("MeasurableId");
					References(x => x.L10Meeting).Column("L10MeetingId");
				}
			}

			public virtual bool _WasModified { get; set; }
		}
		public class L10Meeting_Log : IDeletable, ILongIdentifiable {
			public virtual long Id { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual L10Meeting L10Meeting { get; set; }
			public virtual UserOrganizationModel User { get; set; }
			public virtual String Page { get; set; }
			public virtual DateTime StartTime { get; set; }
			public virtual DateTime? EndTime { get; set; }

			public class L10Meeting_LogMap : ClassMap<L10Meeting_Log> {
				public L10Meeting_LogMap() {
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					Map(x => x.Page);
					Map(x => x.StartTime);
					Map(x => x.EndTime);
					References(x => x.User).Column("UserId");
					References(x => x.L10Meeting).Column("L10MeetingId");
				}
			}
		}

		public class L10Meeting_Connection : IDeletable, ILongIdentifiable {
			public virtual long Id { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual L10Meeting L10Meeting { get; set; }
			public virtual String ConnectionId { get; set; }
			public virtual UserOrganizationModel User { get; set; }

			public virtual int StartSelection { get; set; }
			public virtual int EndSelection { get; set; }
			public virtual string FocusId { get; set; }
			public virtual FocusType FocusType { get; set; }

			public L10Meeting_Connection() {
				CreateTime = DateTime.UtcNow;
			}

			public class L10Meeting_ConnectionMap : ClassMap<L10Meeting_Connection> {
				public L10Meeting_ConnectionMap() {
					Id(x => x.Id);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					Map(x => x.StartSelection);
					Map(x => x.EndSelection);
					Map(x => x.FocusId);
					Map(x => x.FocusType);
					Map(x => x.ConnectionId);
					References(x => x.User).Column("UserId");
					References(x => x.L10Meeting).Column("L10MeetingId");


				}
			}
		}

		public enum ConclusionDataType {
			CompletedIssue,
			OutstandingTodo,
			MeetingHeadline,
			SendEmailSummaryTo,
			Notes,
			WhiteboardFileId,

		}

		public class L10Meeting_ConclusionData : IHistorical {
			public virtual long Id { get; set; }

			public virtual long L10RecurrenceId { get; set; }
			public virtual long L10MeetingId { get; set; }
			public virtual ForModel ForModel { get; set; }
			public virtual ConclusionDataType Type { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			[Obsolete("Use get and set methods")]
			public virtual string Value { get; set; }

			public virtual void SetValue<T>(T value) {
				Value = JsonConvert.SerializeObject(value);
			}
			public virtual T GetValue<T>() {
				return JsonConvert.DeserializeObject<T>(Value);
			}


			[Obsolete("use other")]
			public L10Meeting_ConclusionData() {
				CreateTime = DateTime.UtcNow;
			}

			public L10Meeting_ConclusionData(long l10RecurrenceId, long l10MeetingId, ForModel forModel, ConclusionDataType type) {
				L10RecurrenceId = l10RecurrenceId;
				L10MeetingId = l10MeetingId;
				ForModel = forModel;
				Type = type;
			}

			public static L10Meeting_ConclusionData Create<T>(long l10RecurrenceId, long l10MeetingId, ConclusionDataType type, T value) {
				var res = new L10Meeting_ConclusionData() {
					L10RecurrenceId = l10RecurrenceId,
					L10MeetingId = l10MeetingId,
					Type = type,
				};
				res.SetValue(value);
				return res;
			}

			public class Map : ClassMap<L10Meeting_ConclusionData> {
				public Map() {
					Id(x => x.Id);
					Map(x => x.L10RecurrenceId);
					Map(x => x.L10MeetingId);
					Component(x => x.ForModel).ColumnPrefix("ForModel_");
					Map(x => x.Type).CustomType<ConclusionDataType>();
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					Map(x => x.Value);
				}
			}

		}

		public class L10Meeting_Attendee : IDeletable, ILongIdentifiable {
			public virtual long Id { get; set; }
			public virtual long UserId { get; set; }
			public virtual UserOrganizationModel User { get; set; }
			public virtual long L10MeetingId { get; set; }
			public virtual L10Meeting L10Meeting { get; set; }
			public virtual decimal? Rating { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual bool SeenTodoFireworks { get; set; }

			public virtual string PadId { get; set; }

			public class L10MeetingAttendeeMap : ClassMap<L10Meeting_Attendee> {
				public L10MeetingAttendeeMap() {
					Id(x => x.Id);
					Map(x => x.Rating);
					Map(x => x.DeleteTime);
					Map(x => x.SeenTodoFireworks);
                    Map(x => x.PadId);
                    References(x => x.User).Column("UserId");
					References(x => x.L10Meeting).Column("L10MeetingId");
				}
			}
		}
	}
}
