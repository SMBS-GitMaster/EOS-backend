using System;

namespace RadialReview {
	[AttributeUsage(AttributeTargets.Method)]
	public class TodoAttribute : Attribute {
		public string message;
		public string[] notes;

		public TodoAttribute(string message = null, params string[] toTest) {
			this.message = message;
			this.notes = toTest ?? new string[0];
		}
	}
}