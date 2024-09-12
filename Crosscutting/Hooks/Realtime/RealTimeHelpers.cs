using NHibernate;
using RadialReview.Hubs;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Crosscutting.Hooks.Realtime {
	public static class RealTimeHelpers {


		[Untested("Supply the connection string in a way that SQS can access.")]
		public static string GetConnectionString() {
			return HookData.ToReadOnly().GetData<string>("ConnectionId");
		}

		public static List<long> GetRecurrencesForMeasurable(ISession s, long measurableId) {
			return s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
				.Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
				.Select(x => x.L10Recurrence.Id)
				.List<long>().ToList();
		}


		public static List<long> GetRecurrencesForScore(ISession s, ScoreModel score) {
			return GetRecurrencesForMeasurable(s, score.MeasurableId);
		}

		public static Rock_Data GetRecurrenceRockData(ISession s, long rockId) {
			var rockRecurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
										.Where(x => x.DeleteTime == null && x.ForRock.Id == rockId)
										.Select(x => x.L10Recurrence.Id, x => x.Id)
										.Future<object[]>()
										.Select(x => new Rock_RecurId() {
											RecurrenceId = (long)x[0],
											RecurrenceRockId = (long)x[1]
										});
			return new Rock_Data {
				RecurData = rockRecurrenceIds,
			};
		}

		public class Rock_RecurId {
			public long RecurrenceId { get; internal set; }
			public long RecurrenceRockId { get; internal set; }
		}

		public class Rock_Data {
			public IEnumerable<Rock_RecurId> RecurData { get; set; }

			public List<long> GetRecurrenceIds() {
				return RecurData.Select(x => x.RecurrenceId).ToList();
			}
		}


		public static void DoRecurrenceUpdate(RealTimeUtility rt,ISession s, long recurrenceId, Action<RealTimeUtility.GroupUpdater> action) {
			var meetingHub = rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));
			action(meetingHub);
		}


	}
}
