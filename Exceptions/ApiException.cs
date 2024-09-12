using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading.Tasks;

namespace RadialReview.Exceptions {
	public class ApiException : Exception, ISafeExceptionMessage {

		public const string DEFAULT_ERROR = "Unknown error";
		public const string DEFAULT_ENDPOINT_NOT_FOUND_ERROR = "Unknown endpoint";

		public ApiException() : base(DEFAULT_ERROR) { }

		public ApiException(string safeMessage) : base(safeMessage ?? DEFAULT_ERROR) { }

		public ApiException(Exception wrapper) : base(GenerateSafeMessage(wrapper), wrapper) { }
		public ApiException(string safeMessage, Exception wrapper) : base(safeMessage ?? DEFAULT_ERROR, wrapper) { }


		public static string GenerateSafeMessage(Exception forException) {
			if (forException != null && (forException is ISafeExceptionMessage) && !string.IsNullOrWhiteSpace(forException.Message)) {
				return forException.Message;
			}
			if (forException is InvalidOperationException && forException.Message!=null && forException.Message.Contains("The view") && forException.Message.Contains(" was not found.")) {
				return DEFAULT_ENDPOINT_NOT_FOUND_ERROR;
			}

			return DEFAULT_ERROR;
		}

		public string GetUnsafeStackTrace() {
			return InnerException.NotNull(x => x.StackTrace) ?? StackTrace;
		}

		public string GetUnsafeMessage() {
			return InnerException.NotNull(x => x.Message) ?? Message;
		}
		public string GetUnsafeTypeName() {
			return InnerException.NotNull(x => x.GetType().FullName) ?? typeof(ApiException).FullName;
		}

		public static ApiException ToApiException(Exception e) {
			if (e is ApiException)
				return (ApiException)e;
			return new ApiException(e);
		}

		public static async Task WriteJsonErrorToResponse(ApiException apiException, HttpResponse response, bool includeDebugDetails = false) {
			response.Clear();
			response.StatusCode = (int)(HttpStatusCode.BadRequest);
			if (includeDebugDetails) {
				//Inner exception only available for debugging
				await response.WriteAsJsonAsync(new {
					error = true,
					message = apiException.Message,
					debug = new {
						message = apiException.GetUnsafeMessage(),
						type = apiException.GetUnsafeTypeName(),
						stacktrace = apiException.GetUnsafeStackTrace()
					}
				});
			} else {
				await response.WriteAsJsonAsync(new {
					error = true,
					message = apiException.Message
				});
			}
			await response.CompleteAsync();
		}

	}
}
