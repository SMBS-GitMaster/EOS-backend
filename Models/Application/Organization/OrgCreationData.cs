using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models {
  public class OrgCreationData : ILongIdentifiable {
		public virtual long Id { get; set; }

		[Required(AllowEmptyStrings = false)]
		public virtual string Name { get; set; }

		[Required]
		public virtual bool EnableL10 { get; set; }
		[Required]
		public virtual bool EnableReview { get; set; }
		[Required]
		public virtual bool EnablePeople { get; set; }
		[Required]
		public virtual bool EnableAC { get; set; }
		[Required]
		public virtual bool EnableProcess { get; set; }

		public virtual AccountType AccountType { get; set; }

		public virtual bool StartDeactivated { get; set; }

		public virtual long? AssignedTo { get; set; }

		[Required]
		public virtual string ReferralSource { get; set; }

		public virtual string ReferralData { get; set; }

		public virtual HasCoach HasCoach { get; set; }
		public virtual long? CoachId { get; set; }

		public virtual string ContactFN { get; set; }
		public virtual string ContactLN { get; set; }
		public virtual string ContactPosition { get; set; }
		public virtual EosUserType ContactEosUserType { get; set; }
		public virtual DateTime? TrialEnd { get; set; }

		[EmailAddress]
		public virtual string ContactEmail { get; set; }
		public virtual long OrgId { get; set; }
		public virtual bool EnableZapier { get; set; }
		public virtual bool EnableWhale { get; set; }
		public virtual bool WhaleTermsAccepted { get; set; }
		public virtual bool EnableDocs { get; set; }

		public virtual DateTime? EosStartDate { get; set; }
		public virtual DateTime CreateTime { get; set; }

		public OrgCreationData() {
			AccountType = AccountType.Demo;
			EnableL10 = true;
			EnableAC = true;
			EnableReview = false;
			EnablePeople = false;
			EnableProcess = false;
			TrialEnd = DateTime.UtcNow.Date.AddDays(30);
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<OrgCreationData> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.Name);
				Map(x => x.EnableL10);
				Map(x => x.EnableReview);
				Map(x => x.EnableAC);
				Map(x => x.AccountType);
				Map(x => x.StartDeactivated);

				Map(x => x.AssignedTo);
				Map(x => x.ReferralSource);
				Map(x => x.ReferralData);
				Map(x => x.HasCoach);

				Map(x => x.ContactFN);
				Map(x => x.ContactLN);
				Map(x => x.ContactEmail);
				Map(x => x.ContactEosUserType);
				Map(x => x.CoachId);
				Map(x => x.TrialEnd);
				Map(x => x.EnablePeople);
				Map(x => x.EnableProcess);

				Map(x => x.OrgId);
				Map(x => x.EnableZapier);
				Map(x => x.EnableWhale);
				Map(x => x.WhaleTermsAccepted);
				Map(x => x.EnableDocs);
				Map(x => x.EosStartDate);
				Map(x => x.CreateTime);
			}

		}

		public virtual double? GetEosYears() {
			try {
				if (EosStartDate != null && CreateTime > new DateTime(1970, 0, 0)) {
					return (EosStartDate.Value - CreateTime).TotalDays / 365.0;
				}
			} catch (Exception e) {
			}
			return null;
		}
	}
}
