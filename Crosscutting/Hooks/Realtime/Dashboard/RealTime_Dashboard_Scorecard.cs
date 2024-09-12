using NHibernate;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Realtime.Dashboard {
	public class RealTime_Dashboard_Scorecard : IMeasurableHook, IMeetingMeasurableHook {
		public bool CanRunRemotely() {
			return false;
		}
		public bool AbsorbErrors() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}


		private async Task AddRemoveMeas(long userId, MeasurableModel meas, AngularListType type, List<ScoreModel> scores = null) {
			await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				var group = rt.UpdateGroup(RealTimeHub.Keys.UserId(userId));
				group.Update(new AngularScorecard(-1) {
					Measurables = meas.NotNull(y => AngularList.CreateFrom(type, new AngularMeasurable(y))),
					Scores = scores.NotNull(y => AngularList.Create(type, y.Select(x => {
						var skipUser = true;
						var ascore = new AngularScore(x, null, skipUser);
						if (skipUser && ascore.Measurable != null) {
							ascore.Measurable.Owner = null;
							ascore.Measurable.Admin = null;
						}
						return ascore;
					})))
				});
			}
		}


		public async Task CreateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, List<ScoreModel> createdScores) {
			//add
			await AddRemoveMeas(measurable.AccountableUserId, measurable, AngularListType.ReplaceIfNewer, createdScores);
			await AddRemoveMeas(measurable.AdminUserId, measurable, AngularListType.ReplaceIfNewer, createdScores);
		}

		public async Task DeleteMeasurable(ISession s, MeasurableModel measurable) {
			//remove
			await AddRemoveMeas(measurable.AccountableUserId, measurable, AngularListType.Remove);
			await AddRemoveMeas(measurable.AdminUserId, measurable, AngularListType.Remove);
		}

		public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
			if (updates.AccountableUserChanged) {
				await AddRemoveMeas(updates.OriginalAccountableUserId, measurable, AngularListType.Remove);
				await AddRemoveMeas(measurable.AccountableUserId, measurable, AngularListType.ReplaceIfNewer);
			}
			if (updates.AdminUserChanged) {
				await AddRemoveMeas(updates.OriginalAdminUserId, measurable, AngularListType.Remove);
				await AddRemoveMeas(measurable.AdminUserId, measurable, AngularListType.ReplaceIfNewer);
			}

			if (updates.GoalDirectionChanged || updates.GoalChanged) {
				await AddRemoveMeas(measurable.AccountableUserId, null, AngularListType.ReplaceIfExists, updatedScores);
				await AddRemoveMeas(measurable.AdminUserId, null, AngularListType.ReplaceIfExists, updatedScores);
			}

		}

		public async Task AttachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, L10Recurrence.L10Recurrence_Measurable recurMeasurable) {

		}

		public async Task DetachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId) {
		}
	}
}
