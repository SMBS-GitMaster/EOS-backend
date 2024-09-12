using System.Security.Claims;

namespace RadialReview.Identity {

	public static class IdentityExtensions {
		public static string GetUserId(this ClaimsPrincipal principal) {
			if (principal == null)
				return null;
			var first = principal.FindFirst(ClaimTypes.NameIdentifier);
			return first?.Value;
		}

		public static string GetUserName(this ClaimsPrincipal principal) {
			if (principal == null)
				return null;
			var first = principal.FindFirst(ClaimTypes.Name);
			return first?.Value;
		}
		public static string GetEmail(this ClaimsPrincipal principal) {
			if (principal == null)
				return null;
			var first = principal.FindFirst(ClaimTypes.Email);
			return first?.Value;
		}
		public static bool IsLoggedIn(this ClaimsPrincipal principal) {
			if (principal == null)
				return false;
			return principal.GetUserId() != null;
		}

	}
}