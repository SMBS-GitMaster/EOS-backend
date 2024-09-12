using Cronos;
using Hangfire;
using Hangfire.Batches;
using Hangfire.Batches.States;
using Hangfire.States;
using RadialReview.Crosscutting.CronJobs;
using RadialReview.Crosscutting.Hangfire.Jobs;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Hangfire;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Crosscutting.Hangfire.Executor {
	public class CronExecutor {


		public static void ExecuteRecurringJob(string jobId) {

			var job = ReflectionUtility.GetAllImplementationsOfInterfaceConstructWithDefaultParameters<ICronJob>().FirstOrDefault(x => x.Behavior.RecurringJobId == jobId);

			if (job == null || !job.Behavior.Enabled)
				return;

			var now = DateTime.UtcNow;
			var cron = CronExpression.Parse(job.Behavior.CronExpression);


			//Fallback to guesstimate most recent execution time.
			//This is used on first execution to launch the job.
			var lazyGuestimateDateFallback = new Lazy<DateTime>(() => {
				var date = cron.GetOccurrences(now.AddDays(-365), now, false, true).LastOrDefault();
				if (date == default(DateTime)) {
					date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second);
				}
				return date;
			});


			//Get and atomically update the PrevExecution time
			//The purpose is to both return the last execution, and also adjust the new PrevExecution time.
			//It needs to happen atomically otherwise Hangfire could schedule a duplicate.
			var lastExecution = HangfireJobUtility.GetAndAtomicallyUpdatePrevExecutedTime(JobStorage.Current, jobId, existingDate => _TimeUpdater(now, existingDate, lazyGuestimateDateFallback.Value, cron));

			List<DateTime> occurs;
			if (lastExecution == null) {
				occurs = new List<DateTime>();
				if (job.Behavior.GapExecutionBehavior != GapExecutionBehavior.OnlyMostRecent) {
					occurs.Add(lazyGuestimateDateFallback.Value);
				}
			} else {
				occurs = cron.GetOccurrences(lastExecution.Value, now, false, true).ToList();
				occurs = job.Behavior.GapExecutionBehavior.FilterDates(occurs, now).ToList();
			}


			//if (lastExecution == null) {
			//	//On first execution...
			//	if (job.Behavior.GapExecutionBehavior != GapExecutionBehavior.OnlyMostRecent) {
			//		var date = lazyGuestimateDateFallback.Value;
			//		jobId = BatchJob.StartNew(x => {
			//			AppendJob(x, job, queue, null, date);
			//			//x.Create(() => job.Execute(x.BatchId, date), new EnqueuedState(queue));
			//		}, outerJobName);
			//	}
			//} else {
			//	//Not the first execution..
			//	var 
			var queue = job.Behavior.Queue ?? HangfireQueues.Immediate.CRON;
			if (occurs.Any()) {
				BatchJob.StartNew(outerBatch => {
					/*string prevBatchId = null; DELETED -- schedule all missing crons to execute sequentially.*/
					foreach (var o in occurs) {
						/*prevBatchId =*/ AppendJob(outerBatch, job, queue, /*prevBatchId,*/ o);
					}
				}, "Cron Scheduler: " + job.Behavior.RecurringJobId + " (Scheduling " + occurs.Count + " " + "job".Pluralize(occurs.Count) + ")");
			}
			//}
		}

		private static string AppendJob(IBatchAction outerBatch, ICronJob job, string queue,/* string previousBatchId,*/ DateTime executionTime) {
			var jobDescription = " - " + job.Behavior.RecurringJobId + " [" + executionTime.ToJsMs() + "]";
			//Enqueue the first job immediately, then queue sequentially.
			IBatchState state;
			//if (previousBatchId == null) {
				state = new BatchStartedState();
			/*} else {
				state = new BatchAwaitingState(previousBatchId, BatchContinuationOptions.OnAnyFinishedState);
			}*/

			//Wrap each instance of cron in its own batch.
			var subBatchId = outerBatch.Create(subBatch => {
				subBatch.Create(() => job.Execute(executionTime), new EnqueuedState(queue));
			}, state, jobDescription);
			//Debug.WriteLine("parent,subbatch:" + previousBatchId + ", " + subBatchId);
			return subBatchId;
		}

		private static DateTime? _TimeUpdater(DateTime now, DateTime? existingDate, DateTime bestGuessMostRecentPossibleOccurence, CronExpression cron) {

			if (existingDate == null) {
				//No existing date, use best guess
				return bestGuessMostRecentPossibleOccurence;
			} else {
				//Date exists, supply with most recent occurence.
				var occurs = cron.GetOccurrences(existingDate.Value, now, false, true).ToList();
				if (occurs.Any()) {
					return occurs.Last();
				}
			}
			//Do not change.
			return existingDate;
		}

	}
}
