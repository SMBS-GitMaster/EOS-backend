using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class ActionFilterWrapper : IAsyncActionFilter {

		private IAsyncActionFilter _wrapper;

		public ActionFilterWrapper(IAsyncActionFilter wrapper) {
			_wrapper = wrapper;
		}

		public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			var _logger = (ILogger<ActionFilterWrapper>)context.HttpContext.RequestServices.GetService(typeof(ILogger<ActionFilterWrapper>));

			_logger.LogDebug("Entering " + _wrapper.GetType().Name);
			try {
				return _wrapper.OnActionExecutionAsync(context, next);
			} catch (Exception e) {
				_logger.LogInformation("> Failed " + _wrapper.GetType().FullName + " with " + e.Message);
				throw;

			} finally {
				_logger.LogDebug("Exiting " + _wrapper.GetType().Name);
			}
		}
	}
}
