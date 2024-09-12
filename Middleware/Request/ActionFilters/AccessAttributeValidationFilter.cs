using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using RadialReview.Api;
using RadialReview.Controllers;
using RadialReview.Exceptions;
using RadialReview.Middleware.Request.HttpContextExtensions;
using RadialReview.Middleware.Request.HttpContextExtensions.Permissions;
using RadialReview.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class AccessAttributeValidationFilter : IAsyncActionFilter {
		private AccessLevel GetAccessLevels(ActionExecutingContext context) {

			var methodInfo = ((ControllerActionDescriptor)context.ActionDescriptor).MethodInfo;
			var accessAttributes = methodInfo.GetCustomAttributes(typeof(AccessAttribute), false)
											.Cast<AccessAttribute>()
											.Select(x => x.AccessLevel)
											.ToList();

			var isMvcController = (context.Controller is Controller);
			var isApiController = context.Controller is IApiController;

			if (!isMvcController && isApiController && accessAttributes.Count() == 0) {
				//automatically assume AccessLevel.Any for API methods.
				accessAttributes.Add(AccessLevel.Any);
			}

			if (accessAttributes.Count() == 0) {
				//should have been caught with the AccessAttributeRequiredFilter.
				throw new Exception("Expected an Access attribute, but one was not found.");
			}

			var accessLevel = (AccessLevel)accessAttributes.Max(level => (int)level);
			return accessLevel;
		}


		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {

			var accessLevel = GetAccessLevels(context);

			var httpCtx = context.HttpContext;
			var permissionsOverrides = httpCtx.GetPermissionOverrides();
			//Pre-configure short circuit
			httpCtx.SetAdminShortCircuit(false);

			switch (accessLevel) {
				case AccessLevel.SignedOut:
					var u = httpCtx.User;
					if (u != null && u.Identity.IsAuthenticated)
						throw new LoginException();
					break;
				case AccessLevel.Any:
					break;
				case AccessLevel.User:
					httpCtx.GetUserModel();
					break;
				case AccessLevel.UserOrganization:
					var u1 = httpCtx.GetUser();
					httpCtx.InjectPermissionOverrides(permissionsOverrides);
					LockoutUtility.ProcessLockout(u1);
					break;
				case AccessLevel.Manager:
					var u2 = context.HttpContext.GetUser();
					httpCtx.InjectPermissionOverrides(permissionsOverrides);
					LockoutUtility.ProcessLockout(u2);
					if (!u2.IsManager()) {
						throw new PermissionsException("You must be a " + Config.ManagerName() + " to view this resource.");
					}
					break;
				case AccessLevel.Radial:
					if (!(httpCtx.GetUserModel().IsRadialAdmin || httpCtx.GetUser().IsRadialAdmin)) {
						throw new PermissionsException("You do not have access to this resource.");
					}
					httpCtx.InjectPermissionOverrides(permissionsOverrides);
					httpCtx.SetAdminShortCircuit(true);
					break;
				case AccessLevel.RadialData:
					goto case AccessLevel.Radial;
				default:
					throw new Exception("Unknown Access Type");

			}
			await next();
		}

		
	}
}
