using Microsoft.AspNetCore.Http;
using RadialReview.Exceptions;
using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {

	public class HandlePermissionException : IExceptionHandler {
		public bool CanProcess(ExceptionHandlerContext ctx) {
			return ctx.Exception is PermissionsException;
		}

		public async Task ProcessException(ExceptionHandlerProcessor handlerContext) {
			handlerContext.HttpContext.Response.Clear();
			var ex = handlerContext.Exception as PermissionsException;
			handlerContext.Renderer.ViewData["DurationMS"] = ex.DurationMS;
			await handlerContext.RenderContextAwareMessage(ex.Message);
		}
	}

}