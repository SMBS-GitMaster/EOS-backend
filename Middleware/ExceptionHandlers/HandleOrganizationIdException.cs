using RadialReview.Exceptions;
using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {
	public class HandleOrganizationIdException : IExceptionHandler {
		public bool CanProcess(ExceptionHandlerContext ctx) {
			return ctx.Exception is OrganizationIdException;
		}

		public async Task ProcessException(ExceptionHandlerProcessor handlerContext) {
			var redirectUrl = ((RedirectException)handlerContext.Exception).RedirectUrl;

			//usually we'd supply the redirectUrl
			await handlerContext.Redirect("/Account/Role");
		}
	}
}
