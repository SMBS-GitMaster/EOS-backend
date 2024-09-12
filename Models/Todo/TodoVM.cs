using System;
using System.Collections.Generic;
using RadialReview.Utilities.DataTypes;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.Todo {
	public class TodoVM {
		[Required]
		public long MeetingId { get; set; }
		[Required]
		public long MeasurableId { get; set; }
		public long IssueId { get; set; }
		public long RockId { get; set; }
		public long HeadlineId { get; set; }
		public virtual string PadId { get; set; }
		[Required]
		public long ByUserId { get; set; }
		[Required]
		[Display(Name = "To-do")]
		public String Message { get; set; }
		[Display(Name = "Details")]
		public string Details { get; set; }
		public long RecurrenceId { get; set; }
		[Required]
		[Display(Name = "Who's Accountable")]
		public long[] AccountabilityId {
			get {
				if (_AccountabilityId != null && _AccountabilityId.Length == 1 && _AccountabilityId[0] == 0 && PossibleUsers != null && PossibleUsers.Count >= 1 && PossibleUsers[0] != null)
					return new[] { PossibleUsers[0].id };
				return _AccountabilityId;
			}
			set {_AccountabilityId = value;}
		}

		public long[] _AccountabilityId { get; set; }
		public List<AccountableUserVM> PossibleUsers { get; set; }
		[Display(Name = "Due Date")]
		public DateTime DueDate { get; set; }
		public long? ForModelId { get; set; }
		public string ForModelType { get; set; }
		public TodoVM() {
			DueDate = DateTime.UtcNow.AddDays(7).AddDays(1).AddSeconds(-1);
		}
		public TodoVM(long accountableUserId, TimeSettings timeSettings) : this() {
			AccountabilityId = new[] { accountableUserId };
			if (timeSettings != null) {
				var ts = timeSettings.GetTimeSettings();
				DueDate = ts.ConvertToServerTime(ts.ConvertFromServerTime(DateTime.UtcNow).Date.AddDays(7).AddDays(1).AddSeconds(-1));
			}
		}
	}

	public class AccountableUserVM {
		public long id { get; set; }
		public string name { get; set; }
		public string imageUrl { get; set; }
	}
}
