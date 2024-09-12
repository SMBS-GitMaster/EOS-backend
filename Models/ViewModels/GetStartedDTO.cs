using DocumentFormat.OpenXml.Office2010.ExcelAc;
using System.Collections.Generic;

namespace RadialReview.Models.ViewModels {
	public class GetStartedDTO {
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public string CompanyName { get; set; }
		public bool HaveImplementer { get; set; }
		public string ImplementerName { get; set; }
		public string ReferralSource { get; set; }
		public string ReferralCode { get; set; }
		public string Referral { get; set; }
		public string Phone { get; set; }
    public bool Agree { get; set; }
	}
}