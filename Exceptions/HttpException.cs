using RadialReview.Exceptions;
using System;
using System.Net;

namespace RadialReview {
	public class HttpException : Exception, ISafeExceptionMessage {
		public int Code { get; set; }
		public HttpException(int code, string message) : base(message) {
			Code = code;
		}
		public HttpException(HttpStatusCode code, string message) : this((int)code, message) {
		}
	}
}
