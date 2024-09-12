using RadialReview.Models.Askables;
using System.Collections.Generic;

namespace RadialReview.Models.ViewModels {
	public class TeamResponsibilitiesViewModel {
		public OrganizationTeamModel Team { get; set; }
		public List<ResponsibilityModel> Responsibilities { get; set; }

	}
}