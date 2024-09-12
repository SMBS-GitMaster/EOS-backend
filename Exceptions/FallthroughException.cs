using RadialReview.Core.Properties;
using System;


namespace RadialReview.Exceptions {
	public class FallthroughException : PermissionsException, ISafeExceptionMessage {
		public FallthroughException(String message, bool disableStacktrace = false) : base(message, disableStacktrace) {
		}
		public FallthroughException() : base(ExceptionStrings.DefaultPermissionsException) {
		}
	}
}