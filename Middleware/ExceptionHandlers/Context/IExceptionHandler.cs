using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {

	public interface IExceptionHandler {

		bool CanProcess(ExceptionHandlerContext ctx);
		Task ProcessException(ExceptionHandlerProcessor processor);

	}
}
