using Microsoft.Extensions.Logging;
using RadialReview.Accessors;
using RadialReview.Models.Tasks;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.ScheduledTasks {

	public class ScheduledTaskHandlerData {
		public bool IsTest { get; set; }
		public Uri Url { get; set; }
		public long TaskId { get; set; }
		public string TaskName { get; set; }
		public DateTime Now { get; set; }
		public IHttpClientFactory ClientFactory { get; set; }
		public ILogger Logger { get; set; }
	}

	public interface IScheduledTaskHandler {

		bool ShouldHandle(ScheduledTaskHandlerData data);
		Task Handle(ScheduledTaskHandlerData data);

	}
}
