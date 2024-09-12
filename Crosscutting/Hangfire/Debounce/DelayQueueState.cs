using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace RadialReview.Crosscutting.Hangfire.Debounce {

	public class DelayQueueState : IState {

		static DelayQueueState() {
			GlobalStateHandlers.Handlers.Add(new Handler());
		}

		public static readonly string StateName = "DelayQueueState";

		public DelayQueueState(TimeSpan enqueueIn, string queue) : this(DateTime.UtcNow.Add(enqueueIn), queue) {

		}

		[JsonConstructor]
		public DelayQueueState(DateTime enqueueAt, string queue) {
			Queue = queue;
			EnqueueAt = enqueueAt;
			ScheduledAt = DateTime.UtcNow;
		}

		public string Queue { get; set; }
		public DateTime EnqueueAt { get; }

		[JsonIgnore]
		public DateTime ScheduledAt { get; }


		[JsonIgnore]
		public string Name => StateName;

		public string Reason { get; set; }

		[JsonIgnore]
		public bool IsFinal => false;

		[JsonIgnore]
		public bool IgnoreJobLoadException => false;

		public Dictionary<string, string> SerializeData() {
			return new Dictionary<string, string>
			{
				{ "Queue", Queue },
				{ "EnqueueAt", JobHelper.SerializeDateTime(EnqueueAt) },
				{ "ScheduledAt", JobHelper.SerializeDateTime(ScheduledAt) }
			};
		}

		internal class Handler : IStateHandler {
			public void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction) {
				var scheduledState = context.NewState as DelayQueueState;
				if (scheduledState == null) {
					throw new InvalidOperationException(
						$"`{typeof(Handler).FullName}` state handler can be registered only for the ScheduledQueue state.");
				}
				var timestamp = JobHelper.ToTimestamp(scheduledState.EnqueueAt);
				transaction.AddToSet("delayqueue", context.BackgroundJob.Id, timestamp);
			}

			public void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction) {
				transaction.RemoveFromSet("delayqueue", context.BackgroundJob.Id);
			}

			public string StateName => DelayQueueState.StateName;
		}
	}
}