using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using static RadialReview.Utilities.Config;
using log4net;
using RadialReview.Models;
using System.Linq;

namespace RadialReview.Utilities.Integrations {
	public class ApiResult {

		[JsonProperty("message")]
		public string EvtMessage { set { Message = value; } }

		[JsonProperty("result_message")]
		public string Message { get; set; }

		[JsonProperty("result_output")]
		public string Output { get; set; }

		public string Data { get; set; }

		public bool IsSuccessful => Code == 1 || EventSuccess == 1;

		[JsonProperty("result_code")]
		protected int Code { get; set; }

		[JsonProperty("success")]
		protected int EventSuccess { get; set; }

		protected bool TestMode { get; set; }

		public bool InTestMode() {
			return TestMode;
		}

		public ApiResult SetTestSuccess() {
			EventSuccess = 1;
			Code = 1;
			TestMode = true;
			return this;
		}

	}
}