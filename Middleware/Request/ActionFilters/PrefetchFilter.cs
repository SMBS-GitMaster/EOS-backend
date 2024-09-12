using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using RadialReview.Middleware.Request.HttpContextExtensions.Prefetch;

namespace RadialReview.Middleware.Request.ActionFilters {
	public partial class PrefetchFilter : IAsyncActionFilter {
		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			context.HttpContext.GetPrefetchData();
			await next();
		}

	}
}
