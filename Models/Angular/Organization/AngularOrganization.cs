using RadialReview.Models.Angular.Base;
using RadialReview.Models.Askables;

namespace RadialReview.Models.Angular.Organization {
	public class AngularOrganization : BaseAngular {
		public AngularOrganization() { }
		public AngularOrganization(long id) : base(id) { }
		public AngularOrganization(UserOrganizationModel caller) : this(caller.Organization.Id) {

			Name = caller.Organization.GetName();
			ImageUrl = caller.Organization.GetImageUrl();
			DateFormat = caller.Organization.Settings.DateFormat;
			HasLogo = caller.Organization.GetImageUrl() != ResponsibilityGroupModel.DEFAULT_IMAGE;
			Timezone = new AngularTimezone() { Offset = caller.Organization.GetTimezoneOffset() };
		}

		public string Name { get; set; }
		public string ImageUrl { get; set; }
		public string DateFormat { get; set; }
		public bool HasLogo { get; set; }
		public AngularTimezone Timezone { get; set; }
	}

	public class AngularTimezone {
		public int Offset { get; set; }
	}
}
