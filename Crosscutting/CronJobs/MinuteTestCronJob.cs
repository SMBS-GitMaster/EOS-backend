using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using RadialReview.Crosscutting.Hangfire.Jobs;
using System;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.CronJobs {
	public class MinuteTestCronJob : ICronJob {
		public CronJobBehavior Behavior => new CronJobBehavior(false, "minute-test", Cron.MinuteInterval(1), GapExecutionBehavior.OnlyMostRecent);

		private PerformContext _console;
		private JobInfo _jobInfo;

		public MinuteTestCronJob(PerformContext console, JobInfo jobInfo) {
			_console = console;
			_jobInfo = jobInfo;
		}

		public async Task Execute(DateTime executeTime) {
			_console.WriteLine("Running Minutly");
			_console.WriteLine(executeTime.ToLongDateString());
			_console.WriteLine(executeTime.ToLongTimeString());
		}
	}
}
