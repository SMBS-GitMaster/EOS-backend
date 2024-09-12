using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models.Onboard {
	public class OnboardingUser : ILongIdentifiable {
		public virtual long Id { get; set; }
		#region Entered Data
		public virtual String CurrentPage { get; set; }
		public virtual String FirstName { get; set; }
		public virtual String LastName { get; set; }
		public virtual String Email { get; set; }
		public virtual String Phone { get; set; }
		public virtual String Position { get; set; }
		public virtual String ProfilePicture { get; set; }
		public virtual String CompanyName { get; set; }
		public virtual String CompanyLogo { get; set; }
		public virtual bool? CurrentlyImplementingEOS { get; set; }
		public virtual bool? CurrentlyRunningLevel10Meetings { get; set; }
		public virtual String DesiredOutcome { get; set; }
		public virtual String ImplementerName { get; set; }
		public virtual bool? HaveImpl { get; set; }
		public virtual String Website { get; set; }
		public virtual string PaymentPlan { get; set; }
		public virtual double? EosStartedAgo { get; set; }

		public virtual string Address_1 { get; set; }
		public virtual string Address_2 { get; set; }
		public virtual string City { get; set; }
		public virtual string State { get; set; }
		public virtual string Zip { get; set; }
		public virtual string Country { get; set; }
		public virtual bool DisableEdit { get; set; }
		public virtual string ReferralSource { get; set; }

		public virtual string ReferralCode { get; set; }

		public virtual string Referral { get; set; }
		public virtual string Industry { get; set; }
		public virtual string CustomIndustry { get; set; }
		public virtual string CompanySize { get; set; }
		public virtual bool HasInterestedRevenueProgram { get; set; }
		public virtual bool? Agree { get; set; }

		#endregion
		#region Defined attributes
		[JsonIgnore]
		public virtual String Guid { get; set; }
		[JsonIgnore]
		public virtual DateTime? EosStartTime { get; set; }
		[JsonIgnore]
		public virtual long? OrganizationId { get; set; }
		[JsonIgnore]
		public virtual long? UserId { get; set; }
		#endregion
		#region Timing data
		[JsonIgnore]
		public virtual DateTime StartTime { get; set; }
		[JsonIgnore]
		public virtual DateTime? DeleteTime { get; set; }
		[JsonIgnore]
		public virtual DateTime? ContactCompleteTime { get; set; }
		[JsonIgnore]
		public virtual DateTime? OutcomeCompleteTime { get; set; }
		[JsonIgnore]
		public virtual DateTime? L10CompleteTime { get; set; }
		[JsonIgnore]
		public virtual DateTime? CreditCardCompleteTime { get; set; }
		[JsonIgnore]
		public virtual DateTime? CreateOrganizationTime { get; set; }
		[JsonIgnore]
		public virtual DateTime? OrganizationCompleteTime { get; set; }
		#endregion
		#region User stats
		[JsonIgnore]
		public virtual string UserAgent { get; set; }
		[JsonIgnore]
		public virtual string Languages { get; set; }
		#endregion
		public virtual UserModel _User { get; set; }
		public virtual UserOrganizationModel _UserOrg { get; set; }

		public class Map : ClassMap<OnboardingUser> {
			public Map() {
				Id(x => x.Id);

				Map(x => x.Address_1);
				Map(x => x.Address_2);
				Map(x => x.City);
				Map(x => x.State);
				Map(x => x.Zip);
				Map(x => x.Country);

				Map(x => x.DisableEdit);
				Map(x => x.CompanyLogo);
				Map(x => x.CompanyName);
				Map(x => x.ContactCompleteTime);
				Map(x => x.CreateOrganizationTime);
				Map(x => x.CreditCardCompleteTime);
				Map(x => x.CurrentlyImplementingEOS);
				Map(x => x.CurrentlyRunningLevel10Meetings);
				Map(x => x.OrganizationCompleteTime);
				Map(x => x.CurrentPage);
				Map(x => x.DesiredOutcome);
				Map(x => x.Email);
				Map(x => x.EosStartTime);
				Map(x => x.EosStartedAgo);
				Map(x => x.FirstName);
				Map(x => x.ImplementerName);
				Map(x => x.HaveImpl);
				Map(x => x.L10CompleteTime);
				Map(x => x.LastName);
				Map(x => x.Website);
				Map(x => x.OutcomeCompleteTime);
				Map(x => x.Phone);
				Map(x => x.Position);
				Map(x => x.ProfilePicture);
				Map(x => x.StartTime);
				Map(x => x.OrganizationId);
				Map(x => x.UserId);
				Map(x => x.UserAgent);
				Map(x => x.Languages);
				Map(x => x.DeleteTime);
				Map(x => x.Guid);
				Map(x => x.ReferralSource);
				Map(x => x.ReferralCode);
				Map(x => x.Referral);
				Map(x => x.Industry);
				Map(x => x.CustomIndustry);
				Map(x => x.CompanySize);
				Map(x => x.HasInterestedRevenueProgram);
				Map(x => x.Agree).Nullable();
			}
		}

	}
}