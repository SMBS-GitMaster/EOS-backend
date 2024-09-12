using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {
	public class HandleHttpException : IExceptionHandler {
		public bool CanProcess(ExceptionHandlerContext ctx) {
			return ctx.Exception is HttpException;
		}

		public async Task ProcessException(ExceptionHandlerProcessor handlerContext) {
			var ex = handlerContext.Exception as HttpException;
			handlerContext.HttpContext.Response.StatusCode = ex.Code;
			await handlerContext.RenderContextAwareMessage(ex.Message);
		}
	}
}
