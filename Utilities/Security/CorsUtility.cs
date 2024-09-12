using System;

namespace RadialReview.Utilities.Security {
	public class CorsUtility {
		public static bool UrlIsManaged(Uri uri) {
			if (Config.IsLocal()) {
				return true;
			} else {
 				if (uri.Host.ToLower() == "traction.tools" || uri.Host.ToLower().EndsWith(".traction.tools")
					|| uri.Host.ToLower() == "bloomgrowth.com" || uri.Host.ToLower().EndsWith(".bloomgrowth.com")) {
					return true;
				} else {
					return false;
				}
			}
		}

		public static bool TryGetAllowedOrigin(Uri uri, out string origin) {
			if (UrlIsManaged(uri)) {
				origin = uri.GetLeftPart(UriPartial.Authority);
				return true;
			} else {
				origin = null;
				return false;
			}
		}
	}
}