using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using static RadialReview.Middleware.Request.HttpContextExtensions.HttpContextItems;

namespace RadialReview.Middleware.Request.HttpContextExtensions.EndpointStorage {
	public static class HttpContextItemsEndpont {
		public static HttpContextItemKey ENDPOINT = new HttpContextItemKey("Endpoint");
		public static HttpContextItemKey ROUTE_DATA = new HttpContextItemKey("RouteData");
		public static HttpContextItemKey ACTION_CONTEXT = new HttpContextItemKey("ActionContext");
		public static void StoreEndpoint(this HttpContext ctx) {
			ctx.SetRequestItem(ROUTE_DATA, ctx.GetRouteData());
			ctx.SetRequestItem(ENDPOINT, ctx.GetEndpoint());
			ctx.SetRequestItem(ACTION_CONTEXT, ctx.RequestServices.GetService<IActionContextAccessor>()?.ActionContext);
		}

		public static Endpoint RetrieveEndpoint(this HttpContext ctx) {
			return ctx.GetRequestItem<Endpoint>(ENDPOINT);
		}
		public static RouteData RetrieveRouteData(this HttpContext ctx) {
			return ctx.GetRequestItem<RouteData>(ROUTE_DATA);
		}
		public static RouteData TryRetrieveRouteData(this HttpContext ctx) {
			return ctx.GetRequestItemOrDefault<RouteData>(ROUTE_DATA, null);
		}
	}
}
