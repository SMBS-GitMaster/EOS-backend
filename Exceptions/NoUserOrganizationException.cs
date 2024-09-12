using System;

namespace RadialReview.Exceptions {
	public class NoUserOrganizationException : RedirectException, ISafeExceptionMessage {
		public NoUserOrganizationException(String message) : base(message) {
			RedirectUrl = "/Home/Index";
		}
		public NoUserOrganizationException() : base("Not attached to any organizations") {
			RedirectUrl = "/Home/Index";
		}
	}
}