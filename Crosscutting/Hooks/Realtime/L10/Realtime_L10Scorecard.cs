using FluentNHibernate.Testing.Values;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Scorecard;
using RadialReview.Models.ViewModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Realtime.L10 {
	public class Realtime_L10Scorecard : IScoreHook, IMeasurableHook, IMeetingMeasurableHook {

		public bool CanRunRemotely() {
			return false;
		}
		public bool AbsorbErrors() {
			return false;
		}


		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}


		public async Task PreSaveUpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
			var groupLookup = new Dictionary<string, RealTimeUtility.GroupUpdater>();
			var updateLookup = new Dictionary<string, List<ScoreAndUpdates>>();

			var recursForMeasurable = GetRecurrencesForScores(s, scoreAndUpdates);

			if (scoreAndUpdates.Any(x => x.updates.ValueChanged) ) {
				var connection = RealTimeHelpers.GetConnectionString();
				await using (var rt = RealTimeUtility.Create(connection)) {
					foreach (var sau in scoreAndUpdates) {
						var updates = sau.updates;
						var score = sau.score;
						if (updates.ValueChanged) {
							if (updates.Calculated) {
								connection = null;
							}

							var groupIds = new List<string>();

							if (recursForMeasurable.ContainsKey(score.Measurable.Id)) {
								var recurIds = recursForMeasurable[score.Measurable.Id];
								groupIds.AddRange(recurIds.Select(rid => RealTimeHub.Keys.GenerateMeetingGroupId(rid)));
							}

							groupIds.Add(RealTimeHub.Keys.UserId(score.AccountableUserId));
							groupIds.Add(RealTimeHub.Keys.UserId(score.Measurable.AdminUserId));

							groupIds = groupIds.OrderBy(x => x).ToList();
							var groupKey = string.Join("##", groupIds) + "###" + connection;
							if (!groupLookup.ContainsKey(groupKey)) {
								groupLookup[groupKey] = rt.UpdateGroups(groupIds);
							}




							if (!updateLookup.ContainsKey(groupKey)) {
								updateLookup[groupKey] = new List<ScoreAndUpdates>();
							}
							updateLookup[groupKey].Add(sau);
						}

					}
					foreach (var kv in updateLookup) {
						if (kv.Value.Any()) {
							var group = groupLookup[kv.Key];
							//Must be first..
							group.Call("receiveUpdateScore", kv.Value.Select(x => new AngularScore(x.score, x.updates.AbsoluteUpdateTime, false)).ToList()); //Weekly Meeting (L10) Updater

							foreach (var u in kv.Value.OrderByDescending(x => x.score.ForWeek)) {
								var score = u.score;
								var updates = u.updates;
								var toUpdate = new AngularScore(score, updates.AbsoluteUpdateTime, false);
								toUpdate.DateEntered = score.Measured == null ? Removed.Date() : DateTime.UtcNow;
								toUpdate.Measured = toUpdate.Measured ?? Removed.Decimal();
								group.Update(toUpdate);
							}
						}
					}
				}
			}
		}

		private static DefaultDictionary<long, List<long>> GetRecurrencesForScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
			var measurableIds = scoreAndUpdates.Select(x => x.score.Measurable.Id).Distinct().ToList();
			Dictionary<long, List<long>> result = GetRecurrencesForMeasurables(s, measurableIds);
			return result.ToDefaultDictionary(x => x.Key, x => x.Value, x => new List<long>());

		}

		private static Dictionary<long, List<long>> GetRecurrencesForMeasurables(ISession s, List<long> measurableIds) {
			var result = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
				.Where(x => x.DeleteTime == null)
				.WhereRestrictionOn(x => x.Measurable.Id).IsIn(measurableIds)
				.Select(x => x.Measurable.Id, x => x.L10Recurrence.Id)
				.List<object[]>().ToList().Select(x => new {
					measurableId = (long)x[0],
					recurrenceId = (long)x[1]
				}).GroupBy(x => x.measurableId)
				.ToDictionary(x => x.Key, x => x.Select(y => y.recurrenceId).ToList());
			return result;
		}

		public async Task CreateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates)
		{
		// noop
		}

		public async Task UpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
			//noop			
		}

		public async Task AttachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, L10Recurrence.L10Recurrence_Measurable recurMeasurable) {
			var recurrenceId = recurMeasurable.L10Recurrence.Id;
			var recur = s.Load<L10Recurrence>(recurrenceId);
			var current = L10Accessor._GetCurrentL10Meeting(s, PermissionsUtility.CreateAdmin(s), recurrenceId, true, false, false);
			var skipRealTime = false;

			var scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id).List().ToList();

			await using (var rt = RealTimeUtility.Create()) {
				if (current != null) {
					var ts = recur.Organization.GetTimeSettings();
					ts.Descending = recur.ReverseScorecard;

					DateTime? highlight = current.StartTime;

					try {
						if (recur.MeetingInProgress != null) {
							var meeting = s.Get<L10Meeting>(recur.MeetingInProgress.Value);
							if (meeting.StartTime != null)
								highlight = meeting.StartTime.Value.AddDays(7 * recur.NotNull(x => x.CurrentWeekHighlightShift));
						}
					} catch (Exception e) {
					}

          ts.Period = TimingUtility.GetPeriodByFrequency(measurable.Frequency);

          var weeks = TimingUtility.GetPeriods(ts, recurMeasurable.CreateTime, highlight, true);

          if (measurable.Frequency != Models.Enums.Frequency.DAILY)
          {
					  var additional = await ScorecardAccessor._GenerateScoreModels_AddMissingScores_Unsafe(s, weeks.Select(x => x.ForWeek), measurable.Id.AsList(), scores, frequency: measurable.Frequency);
					  scores.AddRange(additional);
          }

					//make calculated uneditable..
					if (measurable.HasFormula) {
						foreach (var score in scores) {
							score._Editable = false;
						}
					}

					//set the _ordering Value
					if (recurMeasurable._Ordering > 0) {
						foreach (var score in scores) {
							score.Measurable._Ordering = recurMeasurable._Ordering;
						}
					}

					var mm = new L10Meeting.L10Meeting_Measurable() {
						L10Meeting = current,
						Measurable = measurable,
					};
					s.Save(mm);

					if (!skipRealTime) {
						var org = s.Get<OrganizationModel>(current.OrganizationId);
						var settings = org.Settings;
						var sow = settings.WeekStart;
						var offset = org.GetTimezoneOffset();
						var scorecardType = settings.ScorecardPeriod;



            bool showV3Features = VariableAccessor.Get(Variable.Names.V3_SHOW_FEATURES, () => false);
						var rowId = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).RowCount();
						string first,second;
						{
							var row = ViewUtility.RenderPartial("~/Views/L10/partial/ScorecardRow.cshtml", new ScorecardRowVM {
								MeetingId = current.Id,
								RecurrenceId = recurrenceId,
								MeetingMeasurable = mm,
								Scores = scores,
								Weeks = weeks
							});
							row.ViewData["row"] = rowId - 1;
              row.ViewData["v3ShowFeatures"] = showV3Features;

              first = await row.ExecuteAsync();
						}
						{
							var row = ViewUtility.RenderPartial("~/Views/L10/partial/ScorecardRow.cshtml", new ScorecardRowVM {
								MeetingId = current.Id,
								RecurrenceId = recurrenceId,
								MeetingMeasurable = mm,
								Scores = scores,
								Weeks = weeks
							});
							row.ViewData["row"] = rowId - 1;
							row.ViewData["ShowRow"] = false;
              row.ViewData["v3ShowFeatures"] = showV3Features;
              second = await row.ExecuteAsync();
						}
						rt.UpdateRecurrences(recurrenceId).Call("addMeasurable", first, second);
					}
				} else {
          if (measurable.Frequency != Models.Enums.Frequency.DAILY)
          {
            var additional = await ScorecardAccessor._GenerateScoreModels_AddMissingScores_Unsafe(s, DateTime.UtcNow.AsList(), measurable.Id.AsList(), scores, frequency: measurable.Frequency);
            scores.AddRange(additional);
          }
				}

				if (!skipRealTime) {
					rt.UpdateRecurrences(recurrenceId).UpdateScorecard(scores.Where(x => x.Measurable.Id == measurable.Id), null, true, recurrenceId: recurrenceId);
					rt.UpdateRecurrences(recurrenceId).SetFocus("[data-measurable='" + measurable.Id + "'] input:visible:first");
				}
			}
		}

		public async Task DetachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId) {
			await using (var rt = RealTimeUtility.Create()) {
				rt.UpdateRecurrences(recurrenceId).Update(
						new AngularRecurrence(recurrenceId) {
							Scorecard = new AngularScorecard(recurrenceId) {
								Id = recurrenceId,
								Measurables = AngularList.CreateFrom(AngularListType.Remove, new AngularMeasurable(measurable.Id))
							}
						}
					);

				rt.UpdateRecurrences(recurrenceId).Call("removeMeasurable", measurable.Id);
			}
		}

		public async Task CreateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel m, List<ScoreModel> createdScores) {
			//nothing to do
		}

		public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel m, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
			var applySelf = false;
			await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				var recurrenceIds = RealTimeHelpers.GetRecurrencesForMeasurable(s, m.Id);

				var mmid = m.Id;

				var rtRecur = rt.UpdateRecurrences(recurrenceIds);
				var rtUser = rt.UpdateUsers(m.AccountableUserId, m.AdminUserId);

				var skipUser = true;

				//Owner
				if (updates.AccountableUserChanged) {
					skipUser = false;
					rtRecur.Call("updateMeasurable", mmid, "accountable", m.AccountableUser.NotNull(x => x.GetName()), m.AccountableUserId, m.AccountableUser.ImageUrl(true, ImageSize._32));
				}
				if (updates.AdminUserChanged) {
					rtRecur.Call("updateMeasurable", mmid, "admin", m.AdminUser.NotNull(x => x.GetName()), m.AdminUserId, m.AdminUser.ImageUrl(true, ImageSize._32));
				}

				//Cumulative
				if (updates.ShowCumulativeChanged) {
					rtRecur.Call("updateMeasurable", mmid, "showCumulative", m.ShowCumulative);
				}
				if (updates.CumulativeRangeChanged) {
					rtRecur.Call("updateMeasurable", mmid, "cumulativeRange", m.CumulativeRange);
				}

				//Average
				if (updates.ShowAverageChanged) {
					rtRecur.Call("updateMeasurable", mmid, "showAverage", m.ShowAverage);
				}
				if (updates.AverageRangeChanged) {
					rtRecur.Call("updateMeasurable", mmid, "averageRange", m.AverageRange);
				}

				//Recalculate Avgerage/Cumulative
				if ((updates.CumulativeRangeChanged || updates.ShowCumulativeChanged) || (updates.AverageRangeChanged || updates.ShowAverageChanged)) {
					L10Accessor._RecalculateCumulative_Unsafe(s, rt, m, recurrenceIds);
				}

				//Goal
				if (updates.GoalChanged) {
					rtRecur.Call("updateMeasurable", mmid, "target", m.Goal == null ? null : m.Goal.Value.ToString("0.#####"));
				}
				if (updates.AlternateGoalChanged) {
					rtRecur.Call("updateMeasurable", mmid, "altTarget", m.AlternateGoal.NotNull(x => x.Value.ToString("0.#####")) ?? "");
				}
				if (updates.GoalDirectionChanged) {
					rtRecur.Call("updateMeasurable", mmid, "direction", m.GoalDirection.ToSymbol(), m.GoalDirection.ToString());
				}

				//Title
				if (updates.MessageChanged) {
					rtRecur.Call("updateMeasurable", mmid, "title", m.Title);
				}

				//Unit
				if (updates.UnitTypeChanged) {
					applySelf = true;
					rtRecur.Call("updateMeasurable", mmid, "unitType", m.UnitType.ToTypeString(), m.UnitType);
				}

        //HasV3Config
        if (updates.HasV3Config)
        {
          rtRecur.Call("updateMeasurable", mmid, "hasV3Config", m.HasV3Config);
        }

        rtRecur.UpdateMeasurable(m, updatedScores, false, null, false, forceNoSkip: applySelf);
				var am = new AngularMeasurable(m, skipUser);
				if (skipUser) {
					am.Owner = null;
					am.Admin = null;
				}
				rtUser.Update(am);

				if (updates.UpdateAboveWeek != null) {
					rtRecur.Call("updateScoresGoals", updates.UpdateAboveWeek.ToJsMs(), m.Id, new {
						GoalDir = m.GoalDirection,
						Goal = m.Goal,
						AltGoal = m.AlternateGoal,
					});
				}
			}
		}

		public async Task DeleteMeasurable(ISession s, MeasurableModel measurable) {
			//nothing to do
		}

		public async Task RemoveFormula(ISession ses, long measurableId) {
			//noop
		}

		public async Task PreSaveRemoveFormula(ISession s, long measurableId) {
			//noop
			var recurIdsLU = GetRecurrencesForMeasurables(s, measurableId.AsList());

			var measurable = s.Get<MeasurableModel>(measurableId);

			var connection = RealTimeHelpers.GetConnectionString();
			var recurIds = recurIdsLU[measurableId];

			var groupIds = recurIds.Select(rid => RealTimeHub.Keys.GenerateMeetingGroupId(rid)).ToList();

			groupIds.Add(RealTimeHub.Keys.UserId(measurable.AccountableUserId));
			groupIds.Add(RealTimeHub.Keys.UserId(measurable.AdminUserId));
			groupIds = groupIds.OrderBy(x => x).ToList();

			await using (var rt = RealTimeUtility.Create(connection)) {
				rt.UpdateGroups(groupIds).Update(new AngularMeasurable(measurableId) {
					Disabled = false
				});
			}
		}

	}
}

