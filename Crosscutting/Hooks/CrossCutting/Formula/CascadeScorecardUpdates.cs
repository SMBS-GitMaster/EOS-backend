using RadialReview.Utilities.Hooks;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using System.Threading.Tasks;
using RadialReview.Accessors;

namespace RadialReview.Crosscutting.Hooks.CrossCutting.Formula {
	public class CascadeScorecardFormulaUpdates : IScoreHook {
		public bool CanRunRemotely() {
			return false;
		}
		public bool AbsorbErrors() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.High;
		}

		public async Task PreSaveRemoveFormula(ISession s, long measurableId) {
			//noop
		}
		public async Task PreSaveUpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
			//noop
		}
		public async Task RemoveFormula(ISession ses, long measurableId) {
			//noop
		}

		public async Task CreateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates)
		{
		    // noop
		}

    	public async Task UpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
			var scores = scoreAndUpdates.Where(x => x.updates.ValueChanged).Select(x => x.score).ToList();
			await ScorecardAccessor.UpdateCalculatedScores_FromUpdatedScore_Unsafe(s, scores);
		}
	}
}
