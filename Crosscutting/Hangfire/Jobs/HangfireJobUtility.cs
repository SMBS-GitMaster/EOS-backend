using Hangfire.Server;
using Hangfire;
using System;
using Hangfire.Storage;
using Hangfire.Common;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Crosscutting.Hangfire.Jobs {


	/// <summary>
	/// Borrows code from Hangfire.RecurringJobExtensions 
	/// https://github.com/HangfireIO/Hangfire/blob/f87fd22e3661e05eb039fe45cb1aafb5b4c688b5/src/Hangfire.Core/RecurringJobExtensions.cs
	/// 
	/// See also the constructor for Hangfire.RecurringJobEntity
	/// https://github.com/HangfireIO/Hangfire/blob/f87fd22e3661e05eb039fe45cb1aafb5b4c688b5/src/Hangfire.Core/RecurringJobEntity.cs
	/// 
	/// </summary>
	/// 
	public class HangfireJobUtility {
		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);


		public static JobInfo GetJobData(PerformContext pc, string jobId) {
			using (var connection = pc.Storage.GetConnection()) {
				return GetJobData(connection, jobId);
			}
		}

		public static DateTime? GetAndAtomicallyUpdatePrevExecutedTime(JobStorage jobStorage, string recurringJobId, Func<DateTime?, DateTime?> timeUpdater) {
			using (var connection = jobStorage.GetConnection()) {
				using (AcquireDistributedRecurringJobLock(connection, recurringJobId, DefaultTimeout)) {
					var key = $"recurring-job:{recurringJobId}";

					var recurringJob = connection.GetAllEntriesFromHash(key) ?? new Dictionary<string, string>();
					DateTime? prevExecution = GetPropertyValue(recurringJob, "PrevExecution", x => (DateTime?)JobHelper.DeserializeDateTime(x));

					//Get updated value
					var newPrevExecution = timeUpdater(prevExecution);

					if (prevExecution != newPrevExecution) {
						//Apply update
						if (newPrevExecution == null) {
							recurringJob.Remove("PrevExecution");
						} else {
							recurringJob["PrevExecution"] = "" + newPrevExecution.Value.ToJsMs();
						}
						connection.SetRangeInHash(key, recurringJob);
					}
					return prevExecution;
				}
			}
		}

		#region Helpers
		private static JobInfo GetJobData(IStorageConnection connection, string jobId) {
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
			if (jobId == null)
				throw new ArgumentNullException(nameof(jobId));

			Dictionary<string, string> job;
			using (AcquireDistributedJobLock(connection, jobId, DefaultTimeout)) {
				job = connection.GetAllEntriesFromHash($"job:{jobId}");
			}
			var data = new JobInfo();

			if (job != null && job.Count != 0) {
				//Populate job data
				data.JobId = jobId;
				data.CreateTime = GetPropertyValue(job, "CreatedAt", x => JobHelper.DeserializeDateTime(x));
				data.State = GetPropertyValue(job, "State", x => x);

				//Try and get a recurring job from the job.
				if (job.ContainsKey("RecurringJobId") && !string.IsNullOrWhiteSpace(job["RecurringJobId"])) {
					var recurringJobId = job["RecurringJobId"].Trim('\"');
					if (!string.IsNullOrWhiteSpace(recurringJobId)) {
						data.RecurringJobInfo = GetRecurringJobInfo(connection, recurringJobId);
					}
				}
			}
			return data;
		}


		private static RecurringJobInfo GetRecurringJobInfo(IStorageConnection connection, string recurringJobId) {
			var recurJobData = new RecurringJobInfo();
			using (AcquireDistributedRecurringJobLock(connection, recurringJobId, DefaultTimeout)) {
				var recurringJob = connection.GetAllEntriesFromHash($"recurring-job:{recurringJobId}");
				if (recurringJob != null && recurringJob.Count != 0) {
					recurJobData.RecurringJobId = recurringJobId;
					recurJobData.NextExecutionTime = GetPropertyValue(recurringJob, "NextExecution", x => JobHelper.DeserializeDateTime(x));
					recurJobData.CronExpression = GetPropertyValue(recurringJob, "Cron", x => x);
					recurJobData.PrevExecutionTime = GetPropertyValue(recurringJob, "PrevExecution", x => (DateTime?)JobHelper.DeserializeDateTime(x));
					recurJobData.LastJobId = GetPropertyValue(recurringJob, "LastJobId", x => x);
					recurJobData.CurrentExecutionTime = GetPriorOccurrence(recurJobData.CronExpression, recurJobData.NextExecutionTime, TimeZoneInfo.Utc);
					recurJobData.Queue = GetPropertyValue(recurringJob, "Queue", x => x);
				}
			}
			return recurJobData;
		}
		private static DateTime GetPriorOccurrence(string cronExpression, DateTime asOf, TimeZoneInfo tzi) {
			var exp = Cronos.CronExpression.Parse(cronExpression);
			var start = asOf.AddDays(-1);
			var rate = 1.1;
			var iter = 0;
			while (iter < 100) {
				iter += 1;
				var occurs = exp.GetOccurrences(start, asOf, false, false);
				start = start.AddDays(-1 * Math.Pow(rate, iter));
				if (!occurs.Any())
					continue;
				var last = occurs.LastOrDefault();
				if (last != asOf)
					return last;
			}
			throw new Exception("could not get prior occurrence");
		}
		private static T GetPropertyValue<T>(Dictionary<string, string> dict, string paramName, Func<string, T> transform) {
			if (dict!=null && dict.ContainsKey(paramName) && !string.IsNullOrWhiteSpace(dict[paramName])) {
				try {
					return transform(dict[paramName]);
				} catch (Exception e) {
				}
			}
			return default;
		}
		private static IDisposable AcquireDistributedRecurringJobLock(IStorageConnection connection, string recurringJobId, TimeSpan timeout) {
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
			if (recurringJobId == null)
				throw new ArgumentNullException(nameof(recurringJobId));

			return connection.AcquireDistributedLock($"lock:recurring-job:{recurringJobId}", timeout);
		}
		private static IDisposable AcquireDistributedJobLock(IStorageConnection connection, string jobId, TimeSpan timeout) {
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
			if (jobId == null)
				throw new ArgumentNullException(nameof(jobId));

			return connection.AcquireDistributedLock($"job:{jobId}:state-lock", timeout);
		}
		#endregion
	}
}
