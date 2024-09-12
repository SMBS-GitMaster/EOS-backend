using RadialReview.Exceptions;
using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {
	public class HandleApiException : IExceptionHandler {
		public bool CanProcess(ExceptionHandlerContext ctx) {
			return ctx.Exception is ApiException || ctx.Source == ExceptionSource.ApiController;
		}

		public async Task ProcessException(ExceptionHandlerProcessor handlerContext) {
			await handlerContext.RenderApiJson(handlerContext.Exception);
		}
	}
}
