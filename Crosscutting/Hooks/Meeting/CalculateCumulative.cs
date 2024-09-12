using RadialReview.Utilities.Hooks;
using System.Collections.Generic;
using NHibernate;
using RadialReview.Models.Scorecard;
using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Utilities.RealTime;
using RadialReview.Crosscutting.Hooks.Realtime;
using RadialReview.Models;
using System.Linq;

namespace RadialReview.Crosscutting.Hooks.Meeting {
	public class CalculateCumulative : IScoreHook, IMeasurableHook {
		public bool CanRunRemotely() {
			return true;
		}
		public bool AbsorbErrors() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task CreateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates)
		{
		// noop
		}

		public async Task UpdateScore(ISession s, ScoreModel score, IScoreHookUpdates updates) {
			if (ShouldUpdate(score, updates)) {
				await using (var rt = RealTimeUtility.Create()) {
					_UpdateCumulative(s, rt, score.MeasurableId, score.AsList());
				}
			}
		}


		public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel m, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
			if (updates.GoalChanged || updates.CumulativeRangeChanged || updates.ShowCumulativeChanged || updates.AverageRangeChanged || updates.ShowAverageChanged) {
				await using (var rt = RealTimeUtility.Create()) {
					_UpdateCumulative(s, rt, m.Id);
				}
			}
		}
		public async Task UpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
			var allScores = scoreAndUpdates.Where(x => ShouldUpdate(x.score, x.updates)).Select(x => x.score).ToList();
			if (allScores.Any()) {
				await using (var rt = RealTimeUtility.Create()) {
					foreach (var scoresByMeas in allScores.GroupBy(x => x.MeasurableId)) {
						var measurableId = scoresByMeas.Key;
						var scores = scoresByMeas.ToList();
						_UpdateCumulative(s, rt, measurableId, scores);
					}
				}
			}
		}
		public async Task CreateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel m, List<ScoreModel> createdScores) {
			//noop
		}
		public async Task DeleteMeasurable(ISession s, MeasurableModel measurable) {
			//noop
		}


		private bool ShouldUpdate(ScoreModel score, IScoreHookUpdates updates) {
			return updates.ValueChanged && (score.Measurable.ShowCumulative || score.Measurable.ShowAverage);
		}



		private static void _UpdateCumulative(ISession s, RealTimeUtility rt, long measurableId, List<ScoreModel> updatedScores = null) {
			var recurrenceIds = RealTimeHelpers.GetRecurrencesForMeasurable(s, measurableId);
			var measurable = s.Get<MeasurableModel>(measurableId);
			L10Accessor._RecalculateCumulative_Unsafe(s, rt, measurable, recurrenceIds, updatedScores);
			if (measurable.ShowCumulative)
				rt.UpdateUsers(measurable.AccountableUserId, measurable.AdminUserId).Call("updateCumulative", measurableId, measurable._Cumulative.NotNull(y => y.Value.ToString("#,##0.###")));
			if (measurable.ShowAverage)
				rt.UpdateUsers(measurable.AccountableUserId, measurable.AdminUserId).Call("updateAverage", measurableId, measurable._Average.NotNull(y => y.Value.ToString("#,##0.###")));

			if (measurable.ShowCumulative)
				rt.UpdateRecurrences(recurrenceIds).Call("updateCumulative", measurableId, measurable._Cumulative.NotNull(y => y.Value.ToString("#,##0.###")));
			if (measurable.ShowAverage)
				rt.UpdateRecurrences(recurrenceIds).Call("updateAverage", measurableId, measurable._Average.NotNull(y => y.Value.ToString("#,##0.###")));

		}

		public async Task PreSaveUpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
			//noop
		}

		public async Task RemoveFormula(ISession ses, long measurableId) {
			//noop
		}

		public async Task PreSaveRemoveFormula(ISession s, long measurableId) {
			//noop
		}
	}
}
