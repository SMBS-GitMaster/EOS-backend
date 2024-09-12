using Hangfire;
using RadialReview.Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.CronJobs {


	public struct CronJobBehavior {
		public CronJobBehavior(bool enabled, string recurringJobId, string cronExpression, GapExecutionBehavior gapBehavior, string queue = null) {
			Enabled = enabled;
			RecurringJobId = recurringJobId;
			CronExpression = cronExpression;
			GapExecutionBehavior = gapBehavior;
			Queue = queue;
		}

		public bool Enabled { get; }
		public string RecurringJobId { get; }
		public string CronExpression { get; }
		public GapExecutionBehavior GapExecutionBehavior { get; set; }
		public string Queue { get; set; }
	}


	public interface ICronJob {
		CronJobBehavior Behavior { get; }
		Task Execute(DateTime executeTime);
	}

	public enum GapExecutionBehavior {
		FillInGaps,
		OnlyMostRecent,
		OnlyPast24Hours
		//Do not put FutureOnly in here. The Cron will never run the job.
	}

	public static class GapExecutionBehaviorExtensions {

		public static IEnumerable<DateTime> FilterDates(this GapExecutionBehavior gapBehavior, IEnumerable<DateTime> occurs, DateTime? now = null) {
			now = now ?? DateTime.UtcNow;
			switch (gapBehavior) {
				case GapExecutionBehavior.FillInGaps:
					//execute all missing items.
					return occurs;
				case GapExecutionBehavior.OnlyMostRecent:
					//don't fill in the gaps, only select the most recent item
					return occurs.TakeLast(1);
				case GapExecutionBehavior.OnlyPast24Hours:
					//fill in the gaps but only up through 24 hours ago.
					return occurs.Where(x => now.Value.AddHours(-24) <= x);
				default:
					throw new ArgumentOutOfRangeException("" + gapBehavior);
			}

		}
	}
}
