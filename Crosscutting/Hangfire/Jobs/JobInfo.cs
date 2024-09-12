using System;

namespace RadialReview.Crosscutting.Hangfire.Jobs {
	public struct JobInfo {

		public string JobId { get; set; }
		public DateTime CreateTime { get; set; }
		public string State { get; set; }
		public RecurringJobInfo RecurringJobInfo { get; set; }
		public bool IsRecurringJob() {
			return RecurringJobInfo.RecurringJobId != null;
		}
	}

	public struct RecurringJobInfo {
		public string RecurringJobId { get; set; }
		public DateTime NextExecutionTime { get; set; }
		public DateTime CurrentExecutionTime { get; set; }
		public DateTime? PrevExecutionTime { get; set; }
		public string CronExpression { get; set; }
		public string Queue { get; set; }
		public string LastJobId { get; set; }
	}
}
