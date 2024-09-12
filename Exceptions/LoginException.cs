using RadialReview.Core.Properties;
using System;
namespace RadialReview.Exceptions {
	public class LoginException : RedirectException, ISafeExceptionMessage {
		public LoginException(String message, string redirectUrl) : base(message ?? ExceptionStrings.DefaultLoginException) {
			RedirectUrl = redirectUrl;
		}
		public LoginException() : this(ExceptionStrings.DefaultLoginException, null) {
		}
	}
}