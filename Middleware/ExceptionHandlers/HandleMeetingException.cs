using RadialReview.Exceptions.MeetingExceptions;
using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {
	public class HandleMeetingException : IExceptionHandler {
		public bool CanProcess(ExceptionHandlerContext ctx) {
			return ctx.Exception is MeetingException;
		}

		public async Task ProcessException(ExceptionHandlerProcessor handlerContext) {
			var ex = handlerContext.Exception as MeetingException;
			var type = ex.MeetingExceptionType;
			await handlerContext.Redirect("/L10/ErrorMessage");
		}
	}
}
