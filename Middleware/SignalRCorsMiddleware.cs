using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using RadialReview.Utilities.Security;
using System;
using System.Threading.Tasks;

namespace RadialReview.Middleware {
	public class SignalRCorsMiddleware {
		private readonly RequestDelegate _next;

		public SignalRCorsMiddleware(RequestDelegate next) {
			_next = next;
		}

		public async Task Invoke(HttpContext context) {
			var uri = new Uri(context.Request.GetDisplayUrl());
			try {
				if (uri.AbsoluteUri.Contains("/negotiate")) {
					string allowedOrigin = null;
					if (CorsUtility.TryGetAllowedOrigin(uri, out allowedOrigin)) {
						context.Response.Headers.Add("Access-Control-Allow-Origin", allowedOrigin);
						context.Response.Headers.Add("Access-Control-Allow-Methods", "*");
						context.Response.Headers.Add("Access-Control-Allow-Headers", "*");
					}
				}
			} catch (Exception e) {
			}

			await _next(context);
		}
	}
}
