using System;

namespace RadialReview.Utilities.Synchronize {
	public class CancellationException : Exception {
		public CancellationException() { }
		public CancellationException(string message) : base(message) { }
	}
}
