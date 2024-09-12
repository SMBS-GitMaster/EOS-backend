using Microsoft.AspNetCore.Mvc.Filters;
using RadialReview.Api;
using RadialReview.Middleware.Request.HttpContextExtensions;
using RadialReview.Utilities.Synchronize;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class ApiDisableSyncExceptionFilter : IAsyncActionFilter {

		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			if (context.Controller is BaseApiController) {
				context.HttpContext.SetRequestItem(SyncUtil.NO_SYNC_EXCEPTION, true);
			}
			await next();

		}
	}
}
