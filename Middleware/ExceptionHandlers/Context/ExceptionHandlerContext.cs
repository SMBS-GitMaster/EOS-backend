using System;

namespace RadialReview.Middleware.ExceptionHandlers {

	public enum ExceptionSource {
		Unknown,
		MvcController,
		ApiController
	}
	
	public struct ExceptionHandlerContext {
		public ExceptionHandlerContext(Exception exception, ExceptionSource source) {
			Exception = exception;
			Source = source;
		}

		public Exception Exception { get; private set; }
		public ExceptionSource Source { get; private set; }

	}
}
