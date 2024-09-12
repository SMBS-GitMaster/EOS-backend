using Microsoft.AspNetCore.Mvc.Filters;
using RadialReview.Middleware.Request.HttpContextExtensions.Navbar;
using RadialReview.Middleware.Request.HttpContextExtensions.Prefetch;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class NavBarInitializeFilter : IAsyncActionFilter {

		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			var navBar = context.HttpContext.GetNavBar();
			navBar.OrganizationCount = context.HttpContext.GetPrefetchData().UserOrganizationCount;
			await next();
		}
	}
}
