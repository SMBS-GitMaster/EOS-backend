using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RadialReview.Middleware.Request.ActionExecutingContextExtensions {
	public static class ActionExecutingContextRedirect {
		public static void RedirectToAction(this ActionExecutingContext ctx, string action, string controller) {
			_RedirectToAction(ctx, action, controller, null);
		}
		public static void RedirectToAction(this ActionExecutingContext ctx, string action, string controller, object routeValues) {
			_RedirectToAction(ctx, action, controller, routeValues);
		}
		private static void _RedirectToAction(ActionExecutingContext ctx, string action, string controller, object routeValues) {
			ctx.Result = new RedirectToActionResult(action, controller, routeValues);
		}
	}
}
