using RadialReview.Core.Properties;
using System;

namespace RadialReview.Exceptions {
	public class PermissionsException : RedirectException, ISafeExceptionMessage {
		public PermissionsException(String message, bool disableStacktrace = false) : base(message) {
			DisableStacktrace = disableStacktrace;
			StatusCodeOverride = System.Net.HttpStatusCode.Forbidden;
		}

		public PermissionsException() : base(ExceptionStrings.DefaultPermissionsException) {
			StatusCodeOverride = System.Net.HttpStatusCode.Forbidden;
		}

		public int? DurationMS { get; set; }
		private bool skipRevert { get; set; }
		public PermissionsException SkipRevert() {
			skipRevert = true;
			return this;
		}
		public bool ShouldRevert() {
			return !skipRevert;
		}
	}
}