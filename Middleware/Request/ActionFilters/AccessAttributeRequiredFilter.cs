using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using RadialReview.Api;
using RadialReview.Controllers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class AccessAttributeRequiredFilter : IAsyncActionFilter {
		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			if (context.Controller is IApiController) {
				//not required for API
				await next();
			} else {
				var methodInfo = ((ControllerActionDescriptor)context.ActionDescriptor).MethodInfo;
				var accessAttributes = methodInfo.GetCustomAttributes(typeof(AccessAttribute), false).Cast<AccessAttribute>();
				if (accessAttributes.Count() == 0) {
					throw new NotImplementedException("Access attribute missing.");
				}
				await next();
			}
		}
	}
}
