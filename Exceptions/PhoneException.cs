using System;

namespace RadialReview.Exceptions {
	public class PhoneException : Exception, ISafeExceptionMessage {
		public PhoneException(string message) : base(message) {
		}

		public PhoneException() : base("We're sorry, this service is unavailable at this time.") {
		}

		public override string ToString() {
			return Message;
		}
	}
}