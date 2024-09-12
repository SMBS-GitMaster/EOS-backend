using Microsoft.AspNetCore.SignalR;
using RadialReview.Hubs;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.RealTime {
	public partial class RealTimeUtility {
		public class RecurrenceUpdater : BaseUpdater<RecurrenceUpdater> {

			protected List<long> _recurrenceIds = new List<long>();
			protected Dictionary<long, L10Meeting> _recurrenceId_meeting = new Dictionary<long, L10Meeting>();

			public RecurrenceUpdater(IEnumerable<long> recurrences, RealTimeUtility rt) : base(rt) {
				_recurrenceIds = recurrences.Distinct().ToList();
			}

			protected override IClientProxy ConstructProxy(IHubClients<IClientProxy> clients, UpdaterSettings settings, IReadOnlyList<string> excludedConnectionIds) {
				return clients.GroupExcept(settings.GroupKeys.Single(), excludedConnectionIds);
			}

			protected override IEnumerable<UpdaterSettings> GenerateUpdaterSettingsImpl() {
				foreach (var rid in _recurrenceIds) {
					yield return UpdaterSettings.Create("recurrence", RealTimeHub.Keys.GenerateMeetingGroupId(rid), rid);
				}
			}

			public RecurrenceUpdater UpdateMeasurable(MeasurableModel measurable, AngularListType type = AngularListType.ReplaceIfNewer, bool forceNoSkip = false) {
				return Update(() => new AngularMeasurable(measurable), ForceNoSkip(forceNoSkip));
			}

			public RecurrenceUpdater UpdateMeasurable(MeasurableModel measurable, IEnumerable<ScoreModel> scores, bool applyNoUser, DateTime? absoluteUpdateTime, bool reorderScorecard, AngularListType type = AngularListType.ReplaceIfNewer, bool forceNoSkip = false) {
				Update(() => {
					var am = new AngularMeasurable(measurable);
					if (!applyNoUser && am.Admin.NotNull(x => x.Key) == AngularUser.NoUser().Key) {
						am.Admin = null;
					}
					if (!applyNoUser && am.Owner.NotNull(x => x.Key) == AngularUser.NoUser().Key) {
						am.Owner = null;
					}
					return am;
				}, ForceNoSkip(forceNoSkip));
				return UpdateScorecard(scores.Where(x => x.Measurable.Id == measurable.Id), absoluteUpdateTime, reorderScorecard, type);
			}

			/// <summary>
			/// AbsoluteUpdateTime is used to avoid collisions on scores in ui
			/// </summary>
			/// <param name="scores"></param>
			/// <param name="absoluteUpdateTime"></param>
			/// <param name="type"></param>
			/// <returns></returns>
			[Obsolete("Requires at least one score")]
			public RecurrenceUpdater UpdateScorecard(IEnumerable<ScoreModel> scores, DateTime? absoluteUpdateTime, bool reorderScorecard, AngularListType type = AngularListType.ReplaceIfNewer, long recurrenceId = 0) {
				return Update(settings => {
					//UpdateAngular stuff
					var scorecard = new AngularScorecard();
					var measurablesList = new List<AngularMeasurable>();
					var measurablesOrderList = new List<AngularMeasurableOrder>();
					foreach (var m in scores.GroupBy(x => x.Measurable.Id)) {
						var measurable = m.First().Measurable;
						measurablesList.Add(new AngularMeasurable(measurable));
						if (reorderScorecard) {
							measurablesOrderList.Add(new AngularMeasurableOrder(recurrenceId, measurable.Id, measurable._Ordering ?? 0));
						}
						var scoresList = new List<AngularScore>();
						foreach (var ss in scores.Where(x => x.Measurable.Id == measurable.Id)) {

							scoresList.Add(new AngularScore(ss, absoluteUpdateTime, false));
						}
						scorecard.Scores = AngularList.Create<AngularScore>(type, scoresList);
					}
					scorecard.MeasurableOrder = AngularList.Create(type, measurablesOrderList);
					scorecard.Measurables = AngularList.Create(type, measurablesList);

					scorecard.Id = settings.KeyId;
					return scorecard;
				});
			}

			public void SetFocus(string selector) {
				Update(settings => new AngularRecurrence(settings.KeyId) {
					Focus = selector
				});
			}
		}
	}
}
