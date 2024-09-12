using Microsoft.AspNetCore.Mvc.Filters;
using RadialReview.Api;
using RadialReview.Exceptions;
using System;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class ApiErrorHandlerFilter : IAsyncActionFilter {
		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			if (context.Controller is IApiController) {
				try {
					await next();
				} catch (Exception e) {
					//Wraps API errors
					throw new ApiException(e);
				}
			} else {
				//Not part of the API
				await next();
			}
		}
	}
}
