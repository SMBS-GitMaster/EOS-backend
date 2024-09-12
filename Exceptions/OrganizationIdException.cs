using RadialReview.Core.Properties;
using System;


namespace RadialReview.Exceptions {
	public class OrganizationIdException : RedirectException {

		public OrganizationIdException(String message, String redirectUrl) : base(message) {
			RedirectUrl = redirectUrl;
		}
		public OrganizationIdException(String redirectUrl = null) : this(ExceptionStrings.DefaultOrganizationIdException, redirectUrl ?? "/Account/Role") {
			ForceReload = true;
		}
	}
}