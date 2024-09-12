using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using RadialReview.Accessors;
using RadialReview.Api;
using RadialReview.Middleware.ExceptionHandlers;
using RadialReview.Middleware.Request.HttpContextExtensions.EndpointStorage;
using RadialReview.Models.ViewModels.Application;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using log4net;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Middleware {
	public static class ExceptionHandlingMiddleware {
		static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static List<IExceptionHandler> OrderedExceptionHandlers = new List<IExceptionHandler>(){

			//Must be first
			new HandleReadonlyModeExceptions(),

			//API exceptions
			new HandleApiException(),

			//Order doesn't matter that much
			new HandleLoginException(),
			new HandlePotentiallyDangerousException(),
			new HandleOrganizationIdException(),
			new HandlePermissionException(),
			new HandleHttpException(),
			new HandlePageDoesNotExistException(),
			new HandleMeetingException(),
			new HandleAdminSetRoleException(),

			//These are catch-alls and should be at the end.
			new HandleRedirectToActionException(),
			new HandleRedirectException(),

			//This catches everything and must be last.
			new HandleGenericExceptions()
		};

        public static void GlobalExceptionHandler( object sender, UnhandledExceptionEventArgs e ) {
			var exception = e.ExceptionObject as Exception;
			log.Error(@$"GlobalExceptionHandler() received a{(e.IsTerminating ? " terminating" : "n")} unhandled exception. " +
					   (e.IsTerminating ? "The server will crash" : "The exception is not terminating")
			);
			log.Error(exception.Message);
			log.Error(exception.StackTrace);
        }

		public static void ConfigureExceptionHandler(this IApplicationBuilder app) {
			// Configure the global exception event handler
			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.UnhandledException += new UnhandledExceptionEventHandler ( GlobalExceptionHandler );

			app.UseExceptionHandler(appError => {
				appError.Run(async context => {
					try {
						//Try to handle the exception
						Exception ex = null;
						var exceptionCollector = new List<Exception>() { new Exception("unhandled exception") };

						try {
							string errorCode = null;

							//Get most recent exception
							ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>().NotNull(x => x.Error) ?? new Exception("unknown exception");
							exceptionCollector = new List<Exception>() { ex };

							//Setup view renderer
							var view = ViewUtility.RenderView("~/views/Error/Index.cshtml", ex);
							view.ViewData["HasBaseController"] = true;
							view.ViewData["NoAccessCode"] = true;
							view.ViewData["NavBar"] = NavBarViewModel.CreateErrorNav();
							view.ViewData["ErrorCode"] = errorCode ?? "1";
							view.ViewData["Settings"] = SettingsAccessor.GenerateViewSettings(null, "error", false, false, null, TermsCollection.DEFAULT);

							//Get controller-action descriptor
							var controllerActionDescriptor = context.RetrieveEndpoint().Metadata.GetMetadata<ControllerActionDescriptor>();
							var exceptionSource = GetExceptionSource(context, controllerActionDescriptor);


							//Construct context helper
							var handlerProcessor = new ExceptionHandlerProcessor(view, context, ex, controllerActionDescriptor, exceptionSource);


							foreach (var h in OrderedExceptionHandlers) {
								//Can this handler process the exception?
								var ctx = new ExceptionHandlerContext(ex, exceptionSource);

								if (h.CanProcess(ctx)) {
									try {
										//Process the exception.
										await h.ProcessException(handlerProcessor);
										//We succeeded. Leave method.
										return;
									} catch (Exception e1) {
										//really dont want to get here. Lets try other handlers
										exceptionCollector.Add(e1);
									}
								}
							}
						} catch (Exception e) {
							//noop
							exceptionCollector.Add(e);
						}

						//Final fallback.
						context.Response.Clear();
						context.Response.StatusCode = 500;
						if (Config.IsLocal()) {
							await context.Response.WriteAsync("<html><body><h2>Fallback error</h2>");
							foreach (var ee in exceptionCollector) {
								await context.Response.WriteAsync("<h1 style='white-space: pre;'>" + ee.Message + "</h1><pre>" + ee.StackTrace + "</pre><hr/>");
							}
							await context.Response.WriteAsync("</body></html>");
						} else {
							await context.Response.WriteAsync("Unhandled error");
						}
					} finally {
						HibernateSession.CloseCurrentSession();
					}
				});
			});
		}

		private static ExceptionSource GetExceptionSource(HttpContext ctx, ControllerActionDescriptor controllerActionDescriptor) {


			if (controllerActionDescriptor == null)
				return ExceptionSource.Unknown;

			//Order matters.
			if (controllerActionDescriptor.ControllerTypeInfo.IsAssignableTo(typeof(IApiController)))
				return ExceptionSource.ApiController;
			if (ctx.Request != null && ctx.Request.Path.StartsWithSegments(new PathString("/api")))
				return ExceptionSource.ApiController;
			if (controllerActionDescriptor.ControllerTypeInfo.IsAssignableTo(typeof(Controller)))
				return ExceptionSource.MvcController;

			return ExceptionSource.Unknown;
		}

	}
}
