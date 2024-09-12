using System;
using System.Net;
using System.Text;
using RadialReview.Exceptions;
using RadialReview.Core.Properties;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Json {
	public class ResultObject<T> {
		public string ErrorMessage { get; set; }
		public bool Error { get; set; }
		public T Data { get; set; }
	}

	public enum StatusType {
		Success,
		Danger,
		Warning,
		Info,
		Primary,
		@Default
	}

	public class ResultObject {
		public object Object { get; set; }
		public string Message { get; set; }
		public StatusType Status { get; set; }
		private bool _Error { get; set; }
		public string Html { get; set; }
		public string Trace { get; set; }
		public string TraceMessage { get; set; }
		private bool? _Refresh { get; set; }
		public bool IsRowUpdate { get; set; }
		public bool? Revert { get; set; }
		public string Redirect { get; set; }
		private bool? _Silent { get; set; }
		public bool NoErrorReport { get; set; }
		public string MessageType => Status.ToString();
		public Dictionary<string, string> Data { get; set; }
		public bool Refresh {
			get {
				try {
					if (HttpContextHelper.Current != null) {
						var requestRefresh = HttpContextHelper.Current.Request.Query["refresh"].FirstOrDefault();
						if (requestRefresh != null && requestRefresh.ToLower() == "true")
							return true;
						if (requestRefresh != null && requestRefresh.ToLower() == "false")
							return false;
					}
				} catch (Exception) {
				}

				if (_Refresh != null)
					return _Refresh.Value;
				return false;
			}

			set {
				_Refresh = value;
			}
		}

		public bool Error {
			get { return _Error; }
			set {
				_Error = value;
				try {
					if (Error)
						HttpContextHelper.Current.Response.StatusCode = (int)HttpStatusCode.BadRequest;
					else
						HttpContextHelper.Current.Response.StatusCode = (int)HttpStatusCode.OK;
				} catch (Exception) {
				}
			}
		}

		public string Heading {
			get {
				switch (Status) {
					case StatusType.Success:
						return "Success!";
					case StatusType.Danger:
						return "Warning";
					case StatusType.Warning:
						return "Warning";
					case StatusType.Info:
						return "Info";
					case StatusType.Primary:
						return "";
					case StatusType.Default:
						return "";
					default:
						throw new ArgumentOutOfRangeException("Unknown message type");
				}
			}
		}

		public bool Silent {
			get {
				//Show By Default
				if (_Silent != null)
					return _Silent.Value;
				try {
					var requestSilent = HttpContextHelper.Current.Request.Query["silent"].FirstOrDefault();
					//If Url says not to silence, then show..
					if (requestSilent != null) {
						if (requestSilent.ToLower() == "false")
							return false;
						if (requestSilent.ToLower() == "true")
							return true;
					}
				} catch (Exception) {
				}
				return false;
			}

			set {
				_Silent = value;
			}
		}

		public ResultObject NoRefresh() {
			Refresh = false;
			return this;
		}

		public ResultObject ForceRefresh() {
			Refresh = true;
			return this;
		}
		public ResultObject ForceSilent() {
			Silent = true;
			return this;
		}
		public ResultObject ForceNoErrorReport() {
			NoErrorReport = true;
			return this;
		}
		public static ResultObject Success(string message) {
			return new ResultObject(false, message) { Status = StatusType.Success };
		}
		public static ResultObject SilentSuccess(object obj = null) {
			return new ResultObject() { Object = obj, Error = false, Message = "Success", Status = StatusType.Success, Silent = true, };
		}
		public static ResultObject CreateError(string message, object obj = null) {
			return new ResultObject() { Object = obj, Error = true, Message = Capitalize(message), Status = StatusType.Danger };
		}
		public static ResultObject Create(object obj, string message = "Success", StatusType status = StatusType.Success, bool error = false) {
			return new ResultObject() { Object = obj, Error = error, Message = message, Status = status };
		}
		public static ResultObject NoMessage() {
			return new ResultObject() { Error = false, Message = null, Object = null, Status = StatusType.Success };
		}
		public static ResultObject CreateMessage(StatusType status, string message) {
			return new ResultObject() { Error = false, Message = message, Object = null, Status = status };
		}
		public static ResultObject CreateHtml(string html, Dictionary<string, string> data = null) {
			return new ResultObject() { Error = false, Message = "Success", Object = null, Status = StatusType.Success, Html = html, Data = data, Silent = true };
		}

		public override string ToString() {
			return (Error ? "Error:" : "Success:") + Message ?? "";
		}

		public static ResultObject CreateRedirect(string url, string message = null) {
			return new ResultObject() { Error = false, Message = message, Object = null, Status = StatusType.Success, Silent = (message == null), Redirect = url, };
		}

		protected ResultObject() {
			Status = StatusType.Success;
		}
		public ResultObject(Boolean error, string message) {
			Error = error;
			Message = Capitalize(message);
			Status = StatusType.Danger;
		}
		public ResultObject(RedirectException e) {
			Error = true;
			Status = StatusType.Danger;
			Message = Capitalize(e.Message);
#if (DEBUG)
			Trace = e.StackTrace;
#endif
		}

		public ResultObject(Exception e) {
			Error = true;
			Status = StatusType.Danger;
			if (e is RedirectException)
				Message = Capitalize(e.Message);
			else
				Message = Capitalize(ExceptionStrings.AnErrorOccured);
			if (e is PermissionsException) {
				Revert = ((PermissionsException)e).ShouldRevert();
			}

#if (DEBUG || true)
			TraceMessage = Capitalize(e.Message);
			Trace = e.StackTrace;
#endif
		}
		private static string Capitalize(string message) {
			StringBuilder builder = new StringBuilder(message);
			if (builder.Length > 0)
				builder[0] = char.ToUpper(message[0]);
			return builder.ToString();
		}
	}
}
