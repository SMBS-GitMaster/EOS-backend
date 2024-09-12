using System;

namespace RadialReview {
	[AttributeUsage(AttributeTargets.Field)]
	public class DoNotDisplay : Attribute {

		public DoNotDisplay() {
		}

	}
}