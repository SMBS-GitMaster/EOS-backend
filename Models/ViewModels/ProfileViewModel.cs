using RadialReview.Models.Enums;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RadialReview.Models.ViewModels {
	public class ProfileViewModel {
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string ImageUrl { get; set; }
		public int? SendTodoTime { get; set; }
		public ManageUserViewModel Manage { get; set; }
		public List<SelectListItem> PossibleTimes { get; set; }
		public string UserId { get; set; }
		public bool ShowScorecardColors { get; set; }
		public bool ReverseScorecard { get; set; }
		public bool DisableTips { get; set; }
		public string PersonalTextNumber { get; set; }
		public string ServerTextNumber { get; set; }
		public bool LoggedIn { get; set; }
		public long? PhoneActionId { get; set; }
		public bool IsVerified { get; set; }
		public ColorMode ColorMode { get; set; }
		public bool? DarkMode { get; set; }
	}
}