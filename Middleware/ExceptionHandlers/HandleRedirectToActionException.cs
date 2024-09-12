using RadialReview.Exceptions;
using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {
	public class HandleRedirectToActionException : IExceptionHandler {
		public bool CanProcess(ExceptionHandlerContext ctx) {
			return ctx.Exception is RedirectToActionException;
		}

		public async Task ProcessException(ExceptionHandlerProcessor handlerContext) {
			var ex = handlerContext.Exception as RedirectToActionException;
			await handlerContext.Redirect("/" + ex.Controller + "/" + ex.Action);
		}
	}
}
