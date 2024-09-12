using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using RadialReview.Exceptions;
using RadialReview.Middleware.Request.HttpContextExtensions;
using RadialReview.Models;
using RadialReview.Utilities.RealTime;
using System;
using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {
	public class HandleLoginException : IExceptionHandler {

		public bool CanProcess(ExceptionHandlerContext ctx) {
			return ctx.Exception is LoginException;
		}

		public async Task ProcessException(ExceptionHandlerProcessor handlerContext) {
			await ForceSignout(handlerContext.HttpContext);

			var redirectUrl = ((RedirectException)handlerContext.Exception).RedirectUrl;
			if (redirectUrl == null) {
				redirectUrl = handlerContext.HttpContext.Request.GetEncodedPathAndQuery();
			}

			var _signInManager = (SignInManager<UserModel>)handlerContext.HttpContext.RequestServices.GetService(typeof(SignInManager<UserModel>));



			await _signInManager.SignOutAsync();
			await handlerContext.Redirect("/Account/Login?ReturnUrl=" + redirectUrl);
		}

		public async Task ForceSignout(HttpContext ctx) {
			try {
				var uid = ctx.GetUser().Id;
				await using (var rt = RealTimeUtility.Create()) {
					rt.UpdateUsers(uid).Call("logoff");
				}
			} catch (Exception) {
			}

			await ctx.SignOutAsync();

		}
	}
}
