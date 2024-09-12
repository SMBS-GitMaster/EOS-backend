using Hangfire;
using RadialReview.Accessors;
using RadialReview.Crosscutting.Hangfire.Jobs;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.CronJobs {
	public class CloseMeetingCronJob : ICronJob {
		public CronJobBehavior Behavior => new CronJobBehavior(true, "CloseOldMeetings_v2", Cron.Hourly(45), GapExecutionBehavior.OnlyMostRecent);


		private JobInfo _jobInfo;

		public CloseMeetingCronJob(JobInfo jobInfo) {
			_jobInfo = jobInfo;
		}

		public async Task Execute(DateTime executeTime) {
			List<long> ids;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var hours = s.GetSettingOrDefault(Variable.Names.CLOSE_MEETING_AFTER, () => 16.0);
					var time = DateTime.UtcNow.AddHours(-hours);
					ids = s.QueryOver<L10Meeting>()
						.Where(x => x.DeleteTime == null && x.CompleteTime == null && x.CreateTime < time)
						.Select(x => x.L10RecurrenceId)
						.List<long>()
						.Distinct()
						.ToList();
				}
			}
			int i = 0;

			foreach (var id in ids) {
				Scheduler.Enqueue(() => TaskAccessor.CloseMeeting(id, default(INotesProvider)));
			}

		}
	}
}
