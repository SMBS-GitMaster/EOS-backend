using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using RadialReview.Utilities;

namespace RadialReview.Models.L10 {
	public partial class L10Recurrence {
		public enum L10PageType {
			[Display(Name = "Check-In"), EnumMember(Value = "Check-In")]
			Segue = 1,
			[Display(Name = "Metrics" ), EnumMember(Value = "Metrics")]
			Scorecard = 2,
			[Display(Name = "Quarterly Goals" ), EnumMember(Value = "Goals")]
			Rocks = 3,
			[Display(Name = "Headlines"), EnumMember(Value = "Headlines")]
			Headlines = 4,
			[Display(Name = "To-dos" ), EnumMember(Value = "To-dos")]
			Todo = 5,
			[Display(Name = "Issues"), EnumMember(Value = "Issues")]
			IDS = 6,
			[Display(Name = "Wrap-up"), EnumMember(Value = "Wrap-up")]
			Conclude = 7,
			[Display(Name = "Title Page"), EnumMember(Value = "Title Page")]
			Empty = 0,
			[Display(Name = "Notes Box"), EnumMember(Value = "Notes Box")]
			NotesBox = 8,
			[Display(Name = "External Page"), EnumMember(Value = "External Page")]
			ExternalPage = 9,
			[Display(Name = "Whiteboard"), EnumMember(Value = "Whiteboard")]
			Whiteboard = 10,
			[DoNotDisplay]
			Html = 889,
		}

		public class L10Recurrence_Page : BaseModel, ILongIdentifiable, IDeletable {
			public virtual long Id { get; set; }
			[JsonIgnore]
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			[JsonConverter(typeof(StringEnumConverter))]
			public virtual L10PageType PageType { get; set; }
			public virtual bool AutoGen { get; set; }
			[JsonIgnore]
			public virtual string PadId { get; set; }
			[JsonIgnore]
			public virtual string WhiteboardId { get; set; }
			public virtual string Url { get; set; }
			[Required]
			public virtual string Title { get; set; }

      public virtual long TitleNoteId { get; set; }

      public virtual string TitleNoteText { get; set; }

			[RemoveScript]
			public virtual string Subheading { get; set; }
			[Range(typeof(decimal), "0", "500"), DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.##}")]
			public virtual decimal Minutes { get; set; }
			public virtual long L10RecurrenceId { get; set; }
			[JsonIgnore]
			public virtual L10Recurrence L10Recurrence { get; set; }
			public virtual int _Ordering { get; set; }
			[JsonIgnore]
			[Obsolete("Use getter and setter instead")]
			public virtual string _SummaryJson { get; set; }
			/*Hack: Treats all dividers as the same. WasModfided indicates that the ordering has already been set.*/
			public virtual bool _WasModified { get; set; }
			public virtual bool _Used { get; set; }
			public virtual string PageTypeStr {
				get {
					return PageType.ToString();
				}
			}

      public virtual double TimeLastStarted { get; set; }

      public virtual double? TimePreviouslySpentS { get; set; }

      public virtual double? TimeLastPaused { get; set; }

      public virtual double TimeSpentPausedS { get; set; }

      public virtual int CheckInType { get; set; }

      public virtual string IceBreaker { get; set; }

      public virtual bool IsAttendanceVisible { get; set; }

      public virtual string PageTypeMapped
            {
				get
                {
					return MeetingUtility.GetPageTypeMapping(this.PageTypeStr);
				}
            }

			public virtual L10PageSummary GetSummary() {
				if (string.IsNullOrWhiteSpace(_SummaryJson))
					return new L10PageSummary();
				return JsonConvert.DeserializeObject<L10PageSummary>(_SummaryJson);
			}

			public virtual void SetSummary(L10PageSummary summary) {
				if (summary == null)
					_SummaryJson = null;
				_SummaryJson = JsonConvert.SerializeObject(summary);
			}
			public L10Recurrence_Page() {
				CreateTime = DateTime.UtcNow;
				PadId = Guid.NewGuid().ToString();
				Title = "";
				Subheading = "";
				Minutes = 5;
			}

			public class Map : BaseModelClassMap<L10Recurrence_Page> {
				public Map() {
					Id(x => x.Id);
					Map(x => x.PageType);
					Map(x => x.PadId);
					Map(x => x.Url);
					Map(x => x.Title);
					Map(x => x.AutoGen);
					Map(x => x.Minutes);
					Map(x => x.Subheading);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					Map(x => x._Ordering);
					Map(x => x._SummaryJson);
          Map(x => x.TitleNoteId);
          Map(x => x.TitleNoteText);
					Map(x => x.WhiteboardId);
          Map(x => x.TimeLastStarted);
          Map(x => x.TimePreviouslySpentS);
          Map(x => x.TimeLastPaused);
          Map(x => x.TimeSpentPausedS);
          Map(x => x.CheckInType);
          Map(x => x.IceBreaker);
          Map(x => x.IsAttendanceVisible);
          References(x => x.L10Recurrence, "L10RecurrenceId").ReadOnly().LazyLoad();
					Map(x => x.L10RecurrenceId, "L10RecurrenceId");
				}
			}			
		}
	}

	public class L10PageSummary {
		public List<SummaryNotes> SummaryNotes { get; set; }

		public L10PageSummary() {
			SummaryNotes = new List<SummaryNotes>();
		}
	}

	public class SummaryNotes {
		public string Title { get; set; }
		public string PadId { get; set; }

		public SummaryNotes() {
		}

		public SummaryNotes(string title, string padId) {
			Title = title;
			PadId = padId;
		}

	}
}
