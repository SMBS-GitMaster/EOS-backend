using System;

namespace RadialReview.Controllers {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class AccessAttribute : Attribute {
		public AccessLevel AccessLevel { get; set; }

		public bool IgnorePaymentLockout { get; set; }

		public AccessAttribute(AccessLevel level) {
			AccessLevel = level;
		}
	}
}