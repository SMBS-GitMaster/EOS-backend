using Microsoft.AspNetCore.Mvc.Filters;
using System;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Threading.Tasks;
using RadialReview.Middleware.Request.HttpContextExtensions.Prefetch;
using RadialReview.Middleware.Request.ActionExecutingContextExtensions;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class ReadonlyModeFilter : IAsyncActionFilter {
		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			try {
				var actionDisc = ((ControllerActionDescriptor)context.ActionDescriptor);
				var ctrl = actionDisc.ControllerName.ToLower();
				var action = actionDisc.ActionName.ToLower();
				var prefetch = context.HttpContext.GetPrefetchData();

				if (prefetch.ReadOnlyMode) {
					//Redirect us into readonly mode
					try {
						if (ctrl == "l10" && action == "meeting") {
							try {
								//View the meeting
								context.RedirectToAction("meeting", "Readonly", context.HttpContext.Request.RouteValues);
								return;
							} catch (Exception) {
								//View the meeting list
								context.RedirectToAction("Index", "Readonly", null);
								return;
							}
						} else if (//forward some routes to normal endpoints...
							!(ctrl == "readonly") && //readonly 
							!(ctrl == "account") && //login/logoff
							!(ctrl == "admin") && //update readonly mode via variables
							!(ctrl == "home" && action == "index") && //login redirect
							!(ctrl == "reactapp" && action == "index") && //login redirect
							!(ctrl == "issues" && action == "pad") //view issues pad.
						) {
							//most pages land on the meeting list.
							context.RedirectToAction("Index", "Readonly", null);
							return;
						}
					} catch (Exception) {
						//fallback to meeting list.
						context.RedirectToAction("Index", "Readonly", null);
						return;
					}
				} else {
					if (ctrl == "readonly") {
						//Out of readonly mode, put us back in the app.
						try {
							object id = null;
							if (action == "meeting" && context.HttpContext.Request.RouteValues.TryGetValue("id", out id) && id != null) {
								context.RedirectToAction("Meeting", "L10", new {
									id = (string)id
								});
								return;
							} else {
								//most pages land on the home screen
								context.RedirectToAction("Index", "Home", null);
								return;
							}
						} catch (Exception e) {
							//fallback to the home screen
							context.RedirectToAction("Index", "Home", null);
							return;
						}
					}
				}
			} catch (Exception) {
			}

			await next();
		}
	}
}
