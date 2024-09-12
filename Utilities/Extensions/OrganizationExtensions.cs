using RadialReview.Models;

namespace RadialReview {
	public static class OrganizationExtensions {
		public static string GetImage(this OrganizationModel organization) {
			if (organization.Image == null)
				return "/i/placeholder";
			return "/i/" + organization.Image.Id.ToString();
		}
	}
}