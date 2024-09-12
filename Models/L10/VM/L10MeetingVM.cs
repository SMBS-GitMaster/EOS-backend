using System;
using System.Collections.Generic;
using RadialReview.Models.Issues;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using System.Runtime.Serialization;
using RadialReview.Utilities;
using RadialReview.Models.UserModels;
using RadialReview.Models.Enums;
using RadialReview.Models.Rocks;
using System.ComponentModel.DataAnnotations;
using RadialReview.Accessors.Suggestions;
using RadialReview.Utilities.Encrypt;
using static RadialReview.Accessors.L10Accessor;
using Microsoft.AspNetCore.Mvc.Rendering;
using RadialReview.Html;

namespace RadialReview.Models.L10.VM {
	public class L10MeetingVM {
		public class InjectCode {
			public string Hash { get { return Crypto.UniqueHash(Code ?? ""); } }
			public string Code { get; set; }
			public InjectCode(string code) {
				Code = code;
			}
		}

		[DataContract]
		public class WeekVM {
			[Obsolete("Uses local time.")]
			public DateTime DisplayDate { get; set; }
			[DataMember(Name = "StartDate")]
			public DateTime StartDate { get; set; }
			public DateTime ForWeek { get; set; }
			public bool IsCurrentWeek { get; set; }
			public int NumPeriods { get; set; }
			public DateTime LocalDate { get { return StartDate; } }
			[DataMember(Name = "EndDate")]
			public DateTime DataContract_EndDate { get { return StartDate.AddDays(7); } }

			[DataMember(Name = "ForWeek")]
			public long DataContract_Weeks { get { return TimingUtility.GetWeekSinceEpoch(ForWeek); } }
			public WeekVM() {
				NumPeriods = 1;
			}
		}

		public L10Recurrence Recurrence { get; set; }
		public L10Meeting Meeting { get; set; }
		public List<ScoreModel> Scores { get; set; }
		public List<IssueModel.IssueModel_Recurrence> Issues { get; set; }
		public List<TodoModel> Todos { get; set; }
		public List<L10Meeting.L10Meeting_Rock> Rocks { get; set; }
		public List<ProfilePictureVM> MemberPictures { get; set; }
		public List<SelectListItem> Modes { get; set; }
		public long ForOrganizationId { get; set; }
		public List<Milestone> Milestones { get; set; }
		public ScorecardPeriod ScorecardType { get; set; }
		public PeopleHeadlineType HeadlineType { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public List<InjectCode> Inject { get; set; }
		public List<PeopleHeadline> Headlines { get; set; }
		public bool CanEdit { get; set; }
		public bool CanAdmin { get; set; }
		public long VtoId { get; set; }
		public List<WeekVM> Weeks { get; set; }
		public string SelectedPageId { get; set; }
		public string HeadlinesId { get; set; }
		public long[] Attendees { get; set; }
		public bool SendEmail { get; set; }
		public ConcludeSendEmail SendEmailRich { get; set; }
		public bool CloseTodos { get; set; }
		public bool CloseHeadlines { get; set; }
		public bool ShowAdmin { get; set; }
		public bool ShowScorecardChart { get; set; }
		public bool EnableTranscript { get; set; }
		public bool UseNewEmailFormat { get; set; }
		public List<MeetingTranscriptVM> CurrentTranscript { get; set; }
		public IEnumerable<L10Recurrence.L10Recurrence_Connection> Connected { get; set; }
		public bool SeenTodoFireworks { get; set; }
		public bool SharingPeopleAnalyzer { get; set; }
		public List<SuggestionModel> Suggestions { get; set; }
		public ShotClockVM ShotClock { get; set; }
		public bool IsPreview { get { return Meeting.NotNull(x => x.Preview); } }
		public DateTime? MeetingStart {
			get {
				if (Meeting != null) {
					if (Meeting.StartTime != null)
						return Meeting.StartTime.Value; 
				}
				return null;
			}
		}

		public List<SelectListItem> GenerateEmailOptions() {
			return SelectExtensions.ToSelectList(typeof(ConcludeSendEmail), "" + SendEmailRich);
		}

		public L10MeetingVM() {
			StartDate = DateTime.UtcNow;
			EndDate = DateTime.UtcNow;
			Weeks = new List<WeekVM>();
			CurrentTranscript = new List<MeetingTranscriptVM>();
			Headlines = new List<PeopleHeadline>();
			Milestones = new List<Milestone>();
			Connected = new List<L10Recurrence.L10Recurrence_Connection>();
			Inject = new List<InjectCode>();
		}
	}

	public enum ConcludeSendEmail {
		[Display(Name = "No one.")]
		None = 0,
		[Display(Name = "To all attendees.")]
		AllAttendees = 2,
		[Display(Name = "To all that rated the meeting.")]
		AllRaters = 3,
	}

	public class MeetingTranscriptVM {
		public long Id { get; set; }
		public string Message { get; set; }
		public string Owner { get; set; }
		public long Order { get; set; }
	}
}
