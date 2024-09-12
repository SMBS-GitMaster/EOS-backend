using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using RadialReview.Middleware.Request.HttpContextExtensions.EndpointStorage;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class StoreEndpointFilter : IAsyncActionFilter {

		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			context.HttpContext.StoreEndpoint();
			await next();
		}
	}
}
