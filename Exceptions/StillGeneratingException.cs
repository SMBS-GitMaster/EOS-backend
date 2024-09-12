using System;

namespace RadialReview.Exceptions {
	public class StillGeneratingException : Exception, ISafeExceptionMessage {

		public TimeSpan EstimatedRemainder { get; private set; }

		public StillGeneratingException(string message, TimeSpan estimatedRemainder) : base(message) {
			EstimatedRemainder = estimatedRemainder;
		}

		public StillGeneratingException(TimeSpan estimatedRemainder) : this("File is still generating", estimatedRemainder) {			
		}

	}
}
