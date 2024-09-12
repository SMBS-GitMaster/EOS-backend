using log4net.Core;
using log4net.Layout.Pattern;
using System.IO;

namespace RadialReview.Utilities.Logging {
	public sealed class FlattenLogMessage : PatternLayoutConverter {

		public bool ShouldFlatten = true;

		public FlattenLogMessage() {
			IgnoresException = false;
			ShouldFlatten = !Config.IsLocal();
		}

		override protected void Convert(TextWriter writer, LoggingEvent loggingEvent) {
			var message = (loggingEvent.RenderedMessage ?? "");
			if (loggingEvent.ExceptionObject != null) {
				var exceptionStr = loggingEvent.GetExceptionString();
				if (!string.IsNullOrWhiteSpace(exceptionStr)) {
					message += "\n" + exceptionStr;
				}
			}
			if (ShouldFlatten) {
				message = message.Replace("\n", "\\n").Replace("\r", "\\r");
			}
			writer.Write(message);
		}
	}
}
