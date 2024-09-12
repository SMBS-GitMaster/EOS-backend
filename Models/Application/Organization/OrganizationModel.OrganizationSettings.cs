using FluentNHibernate.Mapping;
using RadialReview.Models.Application;
using RadialReview.Models.Components;
using RadialReview.Models.Enums;
using RadialReview.Models.Scorecard;
using RadialReview.Core.Properties;
using RadialReview.Utilities.DataTypes;
using System;
using System.Web.WebPages;

namespace RadialReview.Models {
  public partial class OrganizationModel {
    public class OrganizationSettings : TimeSettings {
			public virtual DayOfWeek WeekStart { get; set; }
			public virtual ScorecardPeriod ScorecardPeriod { get; set; }
			public virtual BrandingType Branding { get; set; }

			public virtual string TimeZoneId { get; set; }
			public virtual bool AutoUpgradePayment { get; set; }
			public virtual bool EmployeesCanViewScorecard { get; set; }
			public virtual bool ManagersCanViewScorecard { get; set; }
			public virtual bool EmployeeCanCreateL10 { get; set; }
			public virtual bool ManagersCanCreateL10 { get; set; }
			public virtual bool ManagersCanViewSubordinateL10 { get; set; }
			public virtual bool ManagersCanEditSubordinateL10 { get; set; }
			public virtual bool ManagersCanEditSelf { get; set; }
			public virtual bool EmployeesCanEditSelf { get; set; }
			public virtual bool UsersCanMoveIssuesToAnyMeeting { get; set; }
			public virtual bool UsersCanSharePHToAnyMeeting { get; set; }
			public virtual bool OnlySeeRocksAndScorecardBelowYou { get; set; }
			public virtual bool AllowAddClient { get; set; }
			public virtual bool EnableZapier { get; set; }
			public virtual bool EnableL10 { get; set; }
			public virtual bool EnableReview { get; set; }
			public virtual bool EnablePeople { get; set; }
			public virtual bool EnableWhale { get; set; }
			public virtual bool WhaleTermsAccepted { get; set; }
			public virtual bool EnableCoreProcess { get; set; }
      public virtual bool EnableBetaButton { get; set; }
      public virtual bool EnableDocs { get; set; }
			public virtual bool DisableAC { get; set; }

			public virtual int? DefaultSendTodoTime { get; set; }

			public virtual bool EnableSurvey { get; set; }

			public virtual ShareVtoPages ShareVtoPages { get; set; }

			public virtual String DateFormat { get; set; }
      public virtual long? V3BusinessPlanId { get; set; }


      public virtual int GetTimezoneOffset() {
				return TimeData.GetTimezoneOffset(TimeZoneId);
			}
			public virtual YearStart YearStart {
				get {
					return new YearStart(this);
				}
			}
			public OrganizationSettings() {
				TimeZoneId = "Central Standard Time";
				WeekStart = DayOfWeek.Sunday;

				ScorecardPeriod = ScorecardPeriod.Weekly;
				EmployeesCanViewScorecard = false;
				ManagersCanViewScorecard = true;
				EmployeeCanCreateL10 = false;
				ManagersCanCreateL10 = true;
				AutoUpgradePayment = true;
				ManagersCanViewSubordinateL10 = true;
				ManagersCanEditSubordinateL10 = false;
				EmployeesCanCreateSurvey = false;
				ManagersCanCreateSurvey = true;
				DefaultSendTodoTime = 14;
				OnlySeeRocksAndScorecardBelowYou = true;
				EnableL10 = false;
				EnableReview = false;
				EnableWhale = false;
				WhaleTermsAccepted = false;
				DisableAC = false;
        EnableBetaButton = false;
        LimitFiveState = true;
				DateFormat = "MM-dd-yyyy";
				RockName = "Goals";

				ManagersCanEditSelf = true;
				EmployeesCanEditSelf = true;
				UsersCanMoveIssuesToAnyMeeting = true;
				UsersCanSharePHToAnyMeeting = true;

				ShareVtoPages = ShareVtoPages.BothFFAndSTFNoIssues;

				PrimaryColor = ColorComponent.TractionBlue();
				TextColor = ColorComponent.TractionBlack();

			}
			public virtual string RockName { get; set; }

			public class OrgSettingsVM : ComponentMap<OrganizationSettings> {
				public OrgSettingsVM() {
					Map(x => x.WeekStart);
					Map(x => x.TimeZoneId);

					Map(x => x.EmployeesCanViewScorecard);
					Map(x => x.ManagersCanViewScorecard);
					Map(x => x.AutoUpgradePayment);
					Map(x => x.EmployeeCanCreateL10);
					Map(x => x.ManagersCanCreateL10);
					Map(x => x.ManagersCanViewSubordinateL10);
					Map(x => x.ManagersCanEditSubordinateL10);
					Map(x => x.ManagersCanEditSelf);
					Map(x => x.EmployeesCanEditSelf);
					Map(x => x.UsersCanMoveIssuesToAnyMeeting);
					Map(x => x.UsersCanSharePHToAnyMeeting);
					Map(x => x.AllowAddClient);
					Map(x => x.EmployeesCanCreateSurvey);
					Map(x => x.ManagersCanCreateSurvey);
					Map(x => x.DefaultSendTodoTime);
					Map(x => x.OnlySeeRocksAndScorecardBelowYou);
					Map(x => x.EnableCoreProcess);
          Map(x => x.EnableBetaButton);
          Map(x => x.EnableL10);
					Map(x => x.EnableReview);
					Map(x => x.EnableSurvey);
					Map(x => x.EnablePeople);
					Map(x => x.EnableWhale).Default("false");
					Map(x => x.WhaleTermsAccepted).Default("false");
					Map(x => x.EnableDocs);
					Map(x => x.DisableUpgradeUsers);
					Map(x => x.LimitFiveState);
					Map(x => x.RockName);
					Map(x => x.DateFormat);
          Map(x => x.V3BusinessPlanId);
					Map(x => x.NumberFormat);
					Map(x => x.ShareVtoPages).CustomType<ShareVtoPages>();
					Map(x => x.Branding).CustomType<BrandingType>();
					Map(x => x.ScorecardPeriod).CustomType<ScorecardPeriod>();
					Map(x => x.StartOfYearMonth).CustomType<Month>();
					Map(x => x.StartOfYearOffset).CustomType<DateOffset>();
					Map(x => x.ImageGuid);
					Component(x => x._PrimaryColor).ColumnPrefix("PrimaryColor_");
					Component(x => x._TextColor).ColumnPrefix("TextColor_");
					Map(x => x.EnableZapier);
				}
			}

			public virtual bool EmployeesCanCreateSurvey { get; set; }
			public virtual bool ManagersCanCreateSurvey { get; set; }
			public virtual Month StartOfYearMonth { get; set; }
			public virtual DateOffset StartOfYearOffset { get; set; }
			public virtual NumberFormat NumberFormat { get; set; }
			public virtual bool LimitFiveState { get; set; }
			public virtual bool DisableUpgradeUsers { get; set; }
			public virtual string ImageGuid { get; set; }
			public virtual ColorComponent _PrimaryColor { get; set; }
			public virtual ColorComponent _TextColor { get; set; }
			public virtual ColorComponent TextColor { get { return _TextColor ?? ColorComponent.TractionBlack(); } set { _TextColor = value; } }
			public virtual ColorComponent PrimaryColor { get { return _PrimaryColor ?? ColorComponent.TractionBlue(); } set { _PrimaryColor = value; } }

			public virtual string GetAngularNumberFormat() {
				return NumberFormat.Angular();
			}

			public virtual String GetDateFormat() {
				return DateFormat ?? "MM-dd-yyyy";
			}

			public ITimeData GetTimeSettings() {
				return new TimeData() {
					Now = DateTime.UtcNow,
					Period = ScorecardPeriod,
					TimezoneOffset = GetTimezoneOffset(),
					WeekStart = WeekStart,
					YearStart = YearStart,
				};
			}
			public bool HasImage() {
				return !string.IsNullOrWhiteSpace(ImageGuid);
			}

			public string GetImageUrl(ImageSize size = ImageSize._64) {
				if (string.IsNullOrWhiteSpace(ImageGuid)) {
					return ConstantStrings.AmazonS3Location + ConstantStrings.ImageOrganizationPlaceholder;
				}

				var suffix = "/" + ImageGuid + ".png";
				if (size == ImageSize._suffix)
					return suffix;
				var s = size.ToString().Substring(1);
				return ConstantStrings.AmazonS3Location + s + suffix;
			}

      public string GetImageUrlV3(ImageSize size = ImageSize._64)
      {
        if (string.IsNullOrWhiteSpace(ImageGuid) || string.IsNullOrEmpty(ImageGuid))
          return null;

        var suffix = "/" + ImageGuid + ".png";
        if (size == ImageSize._suffix)
          return suffix;
        var s = size.ToString().Substring(1);
        return ConstantStrings.AmazonS3Location + s + suffix;
      }
    }

	}
}
