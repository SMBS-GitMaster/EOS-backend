using Hangfire.Annotations;
using Hangfire.Server;
using System;
using System.Collections.Concurrent;
using System.Threading;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire;
using log4net;

namespace RadialReview.Crosscutting.Hangfire.Debounce {
	public class DelayedJobQueueScheduler : IBackgroundProcess {

		public static readonly TimeSpan DefaultPollingDelay = TimeSpan.FromSeconds(1);
		private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromMinutes(1);
		private static readonly int BatchSize = 1000;

		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private readonly ConcurrentDictionary<Type, bool> _isBatchingAvailableCache = new ConcurrentDictionary<Type, bool>();

		private readonly IBackgroundJobStateChanger _stateChanger;
		private readonly TimeSpan _pollingDelay;

		public DelayedJobQueueScheduler() : this(DefaultPollingDelay) {
		}

		public DelayedJobQueueScheduler(TimeSpan pollingDelay)
			: this(pollingDelay, new BackgroundJobStateChanger()) {
		}

		public DelayedJobQueueScheduler(TimeSpan pollingDelay, [NotNull] IBackgroundJobStateChanger stateChanger) {
			if (stateChanger == null)
				throw new ArgumentNullException(nameof(stateChanger));

			_stateChanger = stateChanger;
			_pollingDelay = pollingDelay;
		}

		public void Execute(BackgroundProcessContext context) {
			try {
				if (context == null)
					throw new ArgumentNullException(nameof(context));
				var jobsEnqueued = 0;
				while (EnqueueNextScheduledJobs(context)) {
					jobsEnqueued++;
					if (context.IsStopping) {
						log.Info($"{jobsEnqueued}::  context stopped break.");
						break;
					}
				}

				if (jobsEnqueued != 0) {
					log.Info($"{jobsEnqueued} scheduled job(s) enqueued.");
				}

				context.Wait(_pollingDelay);
			} catch (Exception ex) {
				log.Error($" Hangfire Exception: {ex.Message}", ex);
			}
		}

		public override string ToString() {
			return GetType().Name;
		}

		private bool EnqueueNextScheduledJobs(BackgroundProcessContext context) {
			return UseConnectionDistributedLock(context.Storage, connection => {
				if (IsBatchingAvailable(connection)) {
					var timestamp = JobHelper.ToTimestamp(DateTime.UtcNow);
					var jobIds = ((JobStorageConnection)connection).GetFirstByLowestScoreFromSet("delayqueue", 0, timestamp, BatchSize);

					if (jobIds == null || jobIds.Count == 0)
						return false;

					foreach (var jobId in jobIds) {
						if (context.IsStopping)
							return false;

						var currentState = connection.GetStateData(jobId);
						if (currentState?.Name == null || currentState.Name != DelayQueueState.StateName) {
							return false;
						}

						var candidateQueue = currentState.Data["Queue"] ?? EnqueuedState.DefaultQueue;
						EnqueueBackgroundJob(context, connection, jobId, candidateQueue);
					}
				} else {
					for (var i = 0; i < BatchSize; i++) {
						if (context.IsStopping)
							return false;

						var timestamp = JobHelper.ToTimestamp(DateTime.UtcNow);

						var jobId = connection.GetFirstByLowestScoreFromSet("delayqueue", 0, timestamp);
						if (jobId == null)
							return false;

						var currentState = connection.GetStateData(jobId);
						if (currentState?.Name == null || currentState.Name != DelayQueueState.StateName) {
							return false;
						}

						var candidateQueue = currentState.Data["Queue"] ?? EnqueuedState.DefaultQueue;
						EnqueueBackgroundJob(context, connection, jobId, candidateQueue);
					}
				}

				return true;
			});
		}

		private void EnqueueBackgroundJob(BackgroundProcessContext context, IStorageConnection connection, string jobId, string candidateQueue) {
			var appliedState = _stateChanger.ChangeState(new StateChangeContext(
				context.Storage,
				connection,
				jobId,
				new EnqueuedState { Queue = candidateQueue, Reason = $"Triggered by {ToString()}" },
				new[] { DelayQueueState.StateName },
				CancellationToken.None));

			if (appliedState == null) {
				// When a background job with the given id does not exist, we should
				// remove its id from a schedule manually. This may happen when someone
				// modifies a storage bypassing Hangfire API.
				using (var transaction = connection.CreateWriteTransaction()) {
				transaction.RemoveFromSet("delayqueue", jobId);
					transaction.Commit();
				}
			}
		}

		private bool IsBatchingAvailable(IStorageConnection connection) {
			return _isBatchingAvailableCache.GetOrAdd(
				connection.GetType(),
				type => {
					if (connection is JobStorageConnection storageConnection) {
						try {
							storageConnection.GetFirstByLowestScoreFromSet(null, 0, 0, 1);
						} catch (ArgumentNullException ex) when (ex.ParamName == "key") {
							return true;
						} catch (Exception ex) {
							log.Error($@"An exception was thrown during IsBatchingAvailable call", ex);
						}
					}

					return false;
				});
		}

		private T UseConnectionDistributedLock<T>(JobStorage storage, Func<IStorageConnection, T> action) {
			var resource = "locks:schedulepoller";
			try {
				using (var connection = storage.GetConnection())
				using (connection.AcquireDistributedLock(resource, DefaultLockTimeout)) {
					return action(connection);
				}
			} catch (DistributedLockTimeoutException e) when (e.Resource.EndsWith(resource)) {
				// DistributedLockTimeoutException here doesn't mean that delayed jobs weren't enqueued.
				// It just means another Hangfire server did this work.
				log.Error($@"An exception was thrown during acquiring distributed lock on the {resource} resource within {DefaultLockTimeout.TotalSeconds} seconds. The scheduled jobs have not been handled this time. It will be retried in {_pollingDelay.TotalSeconds} seconds", e);
				return default;
			}
		}
	}
}
