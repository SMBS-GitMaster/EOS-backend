using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {
	public class HandlePotentiallyDangerousException : IExceptionHandler {
		public bool CanProcess(ExceptionHandlerContext ctx) {
			return ctx.Exception.Message.NotNull(x=>x.StartsWith("A potentially dangerous Request.Path value was detected from the client (:)"));
		}

		public async Task ProcessException(ExceptionHandlerProcessor context) {
			await context.Redirect("/Home/Index");
		}
	}
}
