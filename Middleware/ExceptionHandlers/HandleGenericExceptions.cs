using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {
	public class HandleGenericExceptions : IExceptionHandler {
		public bool CanProcess(ExceptionHandlerContext ex) {
			return true;
		}

		public async Task ProcessException(ExceptionHandlerProcessor handlerContext) {
			await handlerContext.RenderContextAwareMessage("An error occurred");
		}
	}
}
