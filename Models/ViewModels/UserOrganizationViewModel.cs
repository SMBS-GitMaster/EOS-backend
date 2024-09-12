using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using RadialReview.Models.Scorecard;
using System.ComponentModel.DataAnnotations;
using RadialReview.Accessors;
using RadialReview.Models.Attributes;
using RadialReview.Models.Angular.Accountability;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RadialReview.Models.ViewModels {
	public class CreateUserOrganizationViewModel {
		#region Creation Variables
		[Required]
		public String FirstName { get; set; }
		[Required]
		public String LastName { get; set; }
		[EmailAddressOrEmpty]
		[Obsolete("Use GetEmail instead")]
		public String Email { get; set; }
		public bool SendEmail { get; set; }
		public bool SetOrgAdmin { get; set; }
		public long OrgId { get; set; }
		public bool IsManager { get; set; }
		public bool IsClient { get; set; }
		public string ClientOrganizationName { get; set; }
		public bool EvalOnly { get; set; }
		public bool PlaceholderOnly { get; set; }
		public bool OnLeadershipTeam { get; set; }
		public string PositionName { get; set; }
		public long[] RecurrenceIds { get; set; }
		public long? NodeId { get; set; }
		public long? ManagerNodeId { get; set; }
		public string PhoneNumber { get; set; }
		public ModalSettings Settings { get; set; }
		public string GetEmail() {
			return (Email ?? "").ToLower().Trim();
		}

		#endregion
		#region VM Variables
		public class ModalSettings {
			public bool DisabledBecauseUnverified { get; set; }
			public bool StrictlyHierarchical { get; set; }
			public AssignableNodesCollection PotentialParents { get; set; }
			public List<SelectListItem> PossibleRecurrences { get; set; }
			public bool HideIsManager { get; set; }
			public bool HideEvalOnly { get; set; }
			public bool HideSend { get; set; }
			public bool HideSetOrgAdmin { get; set; }
			public bool HideOnLeadershipTeam { get; set; }
			public bool LockManager { get; set; }
			public bool LockSeat { get; set; }
			public bool LockPlaceholder { get; set; }
		}

		#endregion
		public CreateUserOrganizationViewModel() {
			Settings = new ModalSettings();
		}
	}

	public class EditUserOrganizationViewModel {
		public long UserId { get; set; }
		public bool IsManager { get; set; }
		public bool? ManagingOrganization { get; set; }
		public bool CanSetManagingOrganization { get; set; }
		public bool? EvalOnly { get; set; }
	}

	public class UserViewModel : ICompletable {
		public UserModel User { get; set; }
		public int ReviewToComplete { get; set; }
		public ICompletionModel GetCompletion(bool split = false) {
			int complete = 1;
			int total = 1;
			if (User != null) {
				complete += (User.ImageGuid != null).ToInt();
				total++;
			}
			return new CompletionModel(complete, total);
		}
	}

	public class UserOrganizationDetails {
		public UserOrganizationDetails() { }
		public long SelfId { get; set; }
		public UserOrganizationModel User { get; set; }
		public List<AngularAccountabilityNode> Seats { get; set; }
		public List<EditRockViewModel> Rocks { get; set; }
		public List<MeasurableModel> Measurables { get; set; }
		public List<MeasurableModel> AdminMeasurables { get; set; }
		public bool ManagingOrganization { get; set; }
		public bool ForceEditable { get; set; }
		public bool CanViewRocks { get; set; }
		public bool CanViewMeasurables { get; set; }
		public bool CanEditUserDetails { get; set; }
		public bool ShowCoachLink { get; set; }
		public string CoachLinkHtml { get; set; }
		public bool Editable {
			get {
				return ForceEditable || ManagingOrganization || User.GetPersonallyManaging() || (User.Organization.Settings.ManagersCanEditSelf && User.Id == SelfId && User.ManagerAtOrganization) || (User.Organization.Settings.EmployeesCanEditSelf && User.Id == SelfId);
			}
		}
	}
}
