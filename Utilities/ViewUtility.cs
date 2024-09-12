using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RadialReview.Middleware.Request.HttpContextExtensions.EndpointStorage;

namespace RadialReview.Utilities {
	public partial class ViewUtility {

		private static IHttpContextAccessor _contextAccessor;
		private static IUrlHelperFactory _urlHelper;
		private static ICompositeViewEngine _viewEngine;
		private static IServiceProvider _serviceProvider;
		private static IServiceScopeFactory _scopeFactory;


		public static void Configure(IHttpContextAccessor httpContextAccessor, IUrlHelperFactory urlHelper, ICompositeViewEngine viewEngine, IServiceProvider serviceProvider, IServiceScopeFactory scopeFactory) {
			_contextAccessor = httpContextAccessor;
			_urlHelper = urlHelper;
			_viewEngine = viewEngine;
			_serviceProvider = serviceProvider;
			_scopeFactory = scopeFactory;
		}

		public static ViewRenderer RenderPartial(string viewPath, object model = null) {
			return Render(viewPath, model, true);
		}

		public static ViewRenderer RenderView(string viewPath, object model = null) {
			return Render(viewPath, model, false);
		}

		public static ViewRenderer Render(string viewPath, object model = null, bool partial = false) {
			var controllerAndContext = CreateController<GenericController>();
			var controller = controllerAndContext.Controller;
			var httpContext = controllerAndContext.HttpContext;
			var actionContext = controllerAndContext.ActionContext;

			ViewEngineResult viewResultMain;
			ViewEngineResult viewResultPartial;
			if (viewPath.EndsWith(".cshtml")) {
				viewResultMain = _viewEngine.GetView(viewPath, viewPath, true);
				viewResultPartial = _viewEngine.GetView(viewPath, viewPath, false);
			} else {
				viewResultMain = _viewEngine.FindView(controller.ControllerContext, viewPath, true);
				viewResultPartial = _viewEngine.FindView(controller.ControllerContext, viewPath, false);
			}

			if (!viewResultMain.Success)
				throw new Exception($"A view with the name '{viewPath}' could not be found");

			return new ViewRenderer(_scopeFactory, partial, viewResultMain, viewResultPartial, httpContext, actionContext, model);
		}

		public class ControllerAndContext<T> where T : Controller, new() {
			public HttpContext HttpContext { get; set; }
			public T Controller { get; set; }
			public ActionContext ActionContext { get; set; }
		}

		private static ControllerAndContext<T> CreateController<T>(RouteData routeData = null) where T : Controller, new() {


			var httpContext = _contextAccessor.HttpContext ?? new DefaultHttpContext();
			var urlHelperFactory = _urlHelper;
			// create a disconnected controller instance
			var controller = new T();
			routeData = routeData ?? httpContext.TryRetrieveRouteData() ?? new RouteData();
			var fakeRoute = new FakeRoute();
			routeData.PushState(fakeRoute, null, null);
			var actionDescriptor = new ControllerActionDescriptor();
			var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

			controller.ControllerContext = new ControllerContext(actionContext);
			return new ControllerAndContext<T>() {
				Controller = controller,
				HttpContext = httpContext,
				ActionContext = actionContext,
			};

		}

		public class FakeRoute : IRouter {
			public FakeRoute() { }

			public IUrlHelper _UrlHelper { get; set; }

			public VirtualPathData GetVirtualPath(VirtualPathContext context) {
				var pathParms = new[] { "area", "controller", "action" };
				var builder = "";

				//build path.
				foreach (var path in pathParms) {
					if (!string.IsNullOrWhiteSpace("" + context.Values[path])) {
						builder += context.Values[path].ToString() + "/";
					}
				}
				//remove trailing slashs
				builder = builder.TrimEnd('/');

				//add query params
				var queryParams = context.Values.Where(x => x.Key != null && !pathParms.Any(y => y == x.Key)).ToList();
				if (queryParams.Any()) {
					builder += "?";
					foreach (var q in queryParams) {
						builder += WebUtility.UrlEncode(q.Key) + "=" + WebUtility.UrlEncode("" + q.Value) + "&";
					}
					builder = builder.TrimEnd('&');
				}

				return new VirtualPathData(this, "/" + builder);
			}

			public Task RouteAsync(RouteContext context) {
				throw new NotImplementedException();
			}
		}

	}
}
