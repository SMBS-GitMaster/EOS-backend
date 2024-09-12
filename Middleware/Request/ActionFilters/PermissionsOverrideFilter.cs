using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using RadialReview.Controllers;
using RadialReview.Middleware.Request.HttpContextExtensions.Permissions;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class PermissionsOverridePaymentLockoutFilter : IAsyncActionFilter {
		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			var methodInfo = ((ControllerActionDescriptor)context.ActionDescriptor).MethodInfo;
			var accessAttributes = methodInfo.GetCustomAttributes(typeof(AccessAttribute), false).Cast<AccessAttribute>();
			
			var permissionsOverrides = context.HttpContext.GetPermissionOverrides();
			permissionsOverrides.IgnorePaymentLockout = accessAttributes.Any(x => x.IgnorePaymentLockout);

			await next();
		}
	}
}
