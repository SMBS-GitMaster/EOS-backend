using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using RadialReview.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Middleware {
	public class W3CLoggerMiddleware {
		private enum W3CFields {
			date = 0,
			time = 1,
			sIp = 2,
			csMethod = 3,
			csUriStem = 4,
			csUriQuery = 5,
			sPort = 6,
			csUsername = 7,
			cIp = 8,
			csUserAgent = 9,
			csReferer = 10,
			scStatus = 11,
			scSubstatus = 12,
			scWin32Status = 13,
			timeTaken = 14,
		}
		private ILogger Logger { get; set; }
		private readonly RequestDelegate _next;
		public W3CLoggerMiddleware(RequestDelegate next, ILogger<W3CLoggerMiddleware> logger) {
			_next = next;
			Logger = logger;
		}


		public async Task Invoke(HttpContext context) {
			Logger.LogDebug("Request beginning:" + context.Request.Path.ToString());

			var requestStart = DateTime.UtcNow;
			await _next(context);
			var requestEnd = DateTime.UtcNow;
			var timeTaken = requestEnd - requestStart;
			var fields = new string[15];

			// 2019-10-24 08:57:25 172.31.46.174 GET /signalr/ping _=1571868304762 80 chill@peoplespace.com 172.31.10.100 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/77.0.3865.120+Safari/537.36 
			// https://traction.tools/L10/Meeting/8015 200 0 0 0
			SetField(fields, W3CFields.date, () => requestEnd.ToString("yyyy-MM-dd"));
			SetField(fields, W3CFields.time, () => requestEnd.ToString("hh:mm:ss.ffff"));
			SetField(fields, W3CFields.sIp, () => context.Features.Get<IHttpConnectionFeature>()?.LocalIpAddress?.ToString());
			SetField(fields, W3CFields.csMethod, () => context.Request.Method);
			SetField(fields, W3CFields.csUriStem, () => context.Request.Path.ToString());
			SetField(fields, W3CFields.csUriQuery, () => context.Request.QueryString.ToString());
			SetField(fields, W3CFields.sPort, () => "?");
			SetField(fields, W3CFields.csUsername, () => context.User.GetEmail() ?? "-");
			SetField(fields, W3CFields.cIp, () => "" + context.Connection.RemoteIpAddress);
			SetField(fields, W3CFields.csUserAgent, () => context.Request.Headers[HeaderNames.UserAgent]);
			SetField(fields, W3CFields.csReferer, () => "" + context.Request.Headers[HeaderNames.Referer]);
			SetField(fields, W3CFields.scStatus, () => "" + context.Response.StatusCode);
			SetField(fields, W3CFields.scSubstatus, () => "?");
			SetField(fields, W3CFields.scWin32Status, () => "?");
			SetField(fields, W3CFields.timeTaken, () => "" + ((int)timeTaken.TotalMilliseconds));

			var line = string.Join(" ", fields.Select(x => x ?? "-"));
			Logger.LogInformation(line);
		}



		private void SetField(string[] fields, W3CFields field, Func<string> value) {
			try {
				var v = value() ?? "";
				if (v != null && v.ToLower().Contains("password")) {
					v = "";
				}
				if (v.Length > 4100) {
					v = v.Substring(0, 4090) + "|||...|||";
				}
				fields[(int)field] = v;
			} catch (Exception e1) {
				try {
					fields[(int)field] = "?";
				} catch (Exception e2) {
				}
			}
		}
	}
}
