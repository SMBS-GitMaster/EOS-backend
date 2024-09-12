using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Payments;
using RadialReview.Models.Scorecard;
using TimeZoneConverter;

namespace RadialReview.Models.ViewModels {
	public class OrganizationViewModel {
		public long Id { get; set; }
		public string OrganizationName { get; set; }
		public bool ManagersCanEdit { get; set; }
		public bool StrictHierarchy { get; set; }
		public bool ManagersCanEditPositions { get; set; }
		public bool AllowAddClient { get; set; }
		public bool SendEmailImmediately { get; set; }
		public bool ManagersCanCreateSurvey { get; set; }
		public bool EmployeesCanCreateSurvey { get; set; }
		public bool ManagersCanRemoveUsers { get; set; }
		public string ImageUrl { get; set; }
		public bool ManagersCanEditSelf { get; set; }
		public bool EmployeesCanEditSelf { get; set; }
		public bool UsersCanMoveIssuesToAnyMeeting { get; set; }
		public bool UsersCanSharePHToAnyMeeting { get; set; }
		public DayOfWeek WeekStart { get; set; }
		public string TimeZone { get; set; }
		public bool OnlySeeRockAndScorecardBelowYou { get; set; }
		public ScorecardPeriod ScorecardPeriod { get; set; }
		public Month StartOfYearMonth { get; set; }
		public DateOffset StartOfYearOffset { get; set; }
		public string DateFormat { get; set; }
		public NumberFormat NumberFormat { get; set; }
		public long AccountabilityChartId { get; set; }
		public string PrimaryColorHex { get; set; }
		public ImageUploadViewModel LogoUrl { get; set; }
		public int? DefaultSendTodoTime { get; set; }
		public List<SelectListItem> PossibleTodoTimes { get; set; }
		public QuarterVM CurrentQuarter { get; set; }
		public bool EnableZapier { get; set; }
		public bool EnableCoreProcess { get; set; }
		public bool ShowJointButton { get; set; }
											 

		public List<SelectListItem> TimeZones {
			get {
				return TZConvert.KnownWindowsTimeZoneIds.Select(x => new SelectListItem() { Text = TZConvert.GetTimeZoneInfo(x).DisplayName, Value = TZConvert.GetTimeZoneInfo(x).Id }).ToList();
			}
		}
		public List<CompanyValueModel> CompanyValues { get; set; }
		public List<RockModel> CompanyRocks { get; set; }
		public List<AboutCompanyAskable> CompanyQuestions { get; set; }
		public string RockName { get; set; }
		public List<PaymentMethodVM> Cards { get; set; }
		public PaymentPlanModel PaymentPlan { get; set; }
		public bool LimitFiveState { get; set; }
		public long? SelectedShareVTORecurrenceId { get; set; }
		public ShareVtoPages ShareVtoPages { get; set; }
		public List<SelectListItem> AllVisibleMeetings { get; set; }
	}
}
