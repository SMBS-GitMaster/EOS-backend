using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using RadialReview.Api;
using RadialReview.Exceptions;
using RadialReview.Models.Json;
using RadialReview.Utilities;
using System;
using System.Net;
using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {
	public enum ResponseType {
		Unknown,
		Partial,
		Json,
		ApiJson,
		Action,
	}

	public class ExceptionHandlerProcessor {
		public ExceptionHandlerProcessor(ViewUtility.ViewRenderer renderer, HttpContext httpContext, Exception exception, ControllerActionDescriptor actionContext, ExceptionSource source) {
			Renderer = renderer;
			HttpContext = httpContext;
			Exception = exception;
			ActionContext = actionContext;
			ExceptionSource = source;
		}

		public ViewUtility.ViewRenderer Renderer { get; private set; }
		public HttpContext HttpContext { get; private set; }
		public Exception Exception { get; private set; }
		private ControllerActionDescriptor ActionContext { get; set; }
		private ExceptionSource ExceptionSource { get; set; }


		public ResponseType GetResponseType() {
			var returnType = ActionContext.MethodInfo.ReturnType;

			//API methods
			if (ExceptionSource == ExceptionSource.ApiController || ActionContext.ControllerTypeInfo.IsAssignableTo(typeof(IApiController)))
				return ResponseType.ApiJson;

			//MVC methods
			if (typeof(JsonResult).IsAssignableFrom(returnType) || (typeof(Task<JsonResult>)).IsAssignableFrom(returnType))
				return ResponseType.Json;
			if (typeof(PartialViewResult).IsAssignableFrom(returnType) || (typeof(Task<PartialViewResult>)).IsAssignableFrom(returnType))
				return ResponseType.Partial;
			if (typeof(IActionResult).IsAssignableFrom(returnType) || (typeof(Task<IActionResult>)).IsAssignableFrom(returnType))
				return ResponseType.Action;
			if (typeof(ActionResult).IsAssignableFrom(returnType) || (typeof(Task<ActionResult>)).IsAssignableFrom(returnType))
				return ResponseType.Action;
			return ResponseType.Unknown;
		}

		public async Task RenderContextAwareMessage(string message = null) {
			//MVC
			switch (GetResponseType()) {
				case ResponseType.Partial:
					await RenderPartialMessage(message);
					break;
				case ResponseType.Json:
					HttpContext.Response.Headers.Remove("Content-Encoding");
					await RenderJson();
					break;
				case ResponseType.ApiJson:
					HttpContext.Response.Headers.Remove("Content-Encoding");
					await RenderApiJson(Exception);
					break;
				default:
					await RenderMvcMessage(message);
					break;
			}
			return;
		}

		public async Task RenderApiJson(Exception exception) {
			var apiException = ApiException.ToApiException(exception);
			await ApiException.WriteJsonErrorToResponse(apiException, HttpContext.Response, Config.IsLocal());
		}

		private async Task RenderJson() {
			var exception = new ResultObject(Exception);
			HttpStatusCode? statusOverride = null;
			if (Exception is RedirectException) {
				var re = ((RedirectException)Exception);
				if (re.Silent != null) {
					exception.Silent = re.Silent.Value;
				}
				exception.NoErrorReport = re.NoErrorReport;
				if (re.ForceReload) {
					exception.Refresh = true;
				}
				if (re.StatusCodeOverride != null) {
					statusOverride = re.StatusCodeOverride.Value;
				}
			}
			HttpContext.Response.Clear();
			HttpContext.Response.StatusCode = (int)(statusOverride ?? HttpStatusCode.InternalServerError);
			await HttpContext.Response.WriteAsJsonAsync(new JsonResult(exception).Value);
			await HttpContext.Response.CompleteAsync();
		}

		private async Task RenderMvcMessage(string message) {
			if (!string.IsNullOrWhiteSpace(message)) {
				Renderer.ViewData["Message"] = message;
			}

			Renderer.UsePartialRenderer = false;
			var html = await Renderer.ExecuteAsync();
			HttpContext.Response.Clear();
			HttpContext.Response.StatusCode = 500;
			await HttpContext.Response.WriteAsync(html);
			await HttpContext.Response.CompleteAsync();
		}

		private async Task RenderPartialMessage(string message) {
			message = message ?? "An error has occurred";
			if (!string.IsNullOrWhiteSpace(message)) {
				Renderer.ViewData["Message"] = message;
			}

			Renderer.UsePartialRenderer = true;
			var html = await Renderer.ExecuteAsync();
			HttpContext.Response.Clear();
			HttpContext.Response.StatusCode = 200; 
			await HttpContext.Response.WriteAsync(html);
			await HttpContext.Response.CompleteAsync();

			if (Config.IsLocal()) {
			}
		}

		public async Task RenderRawHtml(string html) {
			var response = HttpContext.Response;
			response.Clear();
			response.StatusCode = (int)HttpStatusCode.OK;
			await response.WriteAsync(html);
			await HttpContext.Response.CompleteAsync();
		}

		public async Task Redirect(string page) {

			page = page ?? "";
			page = page.Replace(" ", "%20");

			//Might need to handle query parameters here.

			HttpContext.Response.Redirect(page, false);
			await HttpContext.Response.CompleteAsync();
		}

	}
}
