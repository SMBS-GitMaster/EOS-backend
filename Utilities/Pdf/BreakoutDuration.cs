using System;
using System.Diagnostics;

namespace RadialReview.Utilities.Pdf {
	public class TimeoutCheck {
		private Stopwatch Watch;
		public TimeSpan MaxDuration;

		public TimeoutCheck(TimeSpan maxDuration) {
			Watch = Stopwatch.StartNew();
			MaxDuration = maxDuration;
		}

		public void ShouldTimeout() {
			if (Watch.Elapsed > MaxDuration)
				throw new LayoutTimeoutException("Took too long. (" + Watch.ElapsedMilliseconds + "ms)");
		}
	}
}