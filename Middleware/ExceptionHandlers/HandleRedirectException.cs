using RadialReview.Exceptions;
using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {
	public class HandleRedirectException : IExceptionHandler {
		public bool CanProcess(ExceptionHandlerContext ctx) {
			return ctx.Exception is RedirectException;
		}

		public async Task ProcessException(ExceptionHandlerProcessor handlerContext) {
			var ex = handlerContext.Exception as RedirectException;
			var returnUrl = ex.RedirectUrl;
			await handlerContext.Redirect("/Error/Index?returnUrl=" + returnUrl);
		}
	}
}
