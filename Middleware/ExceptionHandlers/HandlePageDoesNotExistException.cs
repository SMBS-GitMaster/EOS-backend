using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {
	public class HandlePageDoesNotExistException : IExceptionHandler {
		public bool CanProcess(ExceptionHandlerContext ctx) {
			return ctx.Exception.Message.NotNull(x=>x.StartsWith("A public action method '"));
		}

		public async Task ProcessException(ExceptionHandlerProcessor handlerContext) {
			await handlerContext.RenderContextAwareMessage("Page does not exist");
		}
	}
}
