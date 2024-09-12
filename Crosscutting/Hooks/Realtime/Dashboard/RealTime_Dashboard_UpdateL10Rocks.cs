using NHibernate;
using System.Linq;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.L10;
using System.Threading.Tasks;
using System.Collections.Generic;
using RadialReview.Models.Askables;
using RadialReview.Utilities.Hooks;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.Application;
using RadialReview.Utilities.RealTime;
using HotChocolate.Subscriptions;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Core.GraphQL.Common.Constants;

namespace RadialReview.Crosscutting.Hooks.Realtime {
	public class RealTime_Dashboard_UpdateL10Rocks : IRockHook, IMeetingRockHook {
        public bool CanRunRemotely() {
			return false;
		}
		public bool AbsorbErrors() {
			return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
			var data = RealTimeHelpers.GetRecurrenceRockData(s, rock.Id);
			foreach (var recur in data.GetRecurrenceIds()) {
				await RemoveRock(s, recur, rock.Id, null);
			}

			await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				var group = rt.UpdateGroup(RealTimeHub.Keys.UserId(rock.ForUserId));
				group.Update(new ListDataVM(rock.ForUserId) {
					Rocks = AngularList.CreateFrom(AngularListType.Remove, new AngularRock(rock.Id))
				});
			}
        }

		public async Task AttachRock(ISession s, UserOrganizationModel caller, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock) {
			await AddRock(s, recurRock.L10Recurrence.Id, recurRock);
		}

		public async Task DetachRock(ISession s, RockModel rock, long recurrenceId, IMeetingRockHookUpdates updates) {
			await RemoveRock(s, recurrenceId, rock.Id, rock._Origins, rock._Origins == null || !rock._Origins.Any());
        }

		public async Task CreateRock(ISession s, UserOrganizationModel caller, RockModel rock) {
			await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				var group = rt.UpdateGroup(RealTimeHub.Keys.UserId(rock.ForUserId));
				group.Update(new ListDataVM(rock.ForUserId) {
					Rocks = AngularList.CreateFrom(AngularListType.Add, new AngularRock(rock, null))
				});
			}
        }


		public async Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates) {

			await using (var rt = RealTimeUtility.Create()) {
				if (updates.AccountableUserChanged) {
					var group = rt.UpdateGroup(RealTimeHub.Keys.UserId(updates.OriginalAccountableUserId));
					group.Update(new ListDataVM(updates.OriginalAccountableUserId) {
						Rocks = AngularList.CreateFrom(AngularListType.Remove, new AngularRock(rock.Id))
					});
				}
				rt.UpdateGroup(RealTimeHub.Keys.UserId(rock.ForUserId)).Update(new ListDataVM(rock.ForUserId) {
					Rocks = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularRock(rock, null))
				});
			}
        }

		#region Helpers



		private async Task AddRock(ISession s, long recurrenceId, L10Recurrence.L10Recurrence_Rocks rock) {
			await using (var rt = RealTimeUtility.Create()) {
				RealTimeHelpers.DoRecurrenceUpdate(rt, s, recurrenceId, x => {
					x.Update(new AngularTileId<IEnumerable<AngularRock>>(0, recurrenceId, null, AngularTileKeys.L10RocksList(recurrenceId)) {
						Contents = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularRock(rock) {
							Archived = false,
						})
					});
				});
			}
        }

		private async Task RemoveRock(ISession s, long recurrenceId, long rockId, IEnumerable<NameId> origins, bool setArchive = true) {
			await using (var rt = RealTimeUtility.Create()) {
				RealTimeHelpers.DoRecurrenceUpdate(rt, s, recurrenceId, x => {
					x.Update(new AngularTileId<IEnumerable<AngularRock>>(0, recurrenceId, null, AngularTileKeys.L10RocksList(recurrenceId)) {
						Contents = AngularList.CreateFrom(AngularListType.ReplaceIfExists, new AngularRock(rockId) {
							Archived = setArchive,
							Origins = origins.NotNull(x => AngularList.Create(AngularListType.ReplaceAll, x))
						})
					});
				});
			}
        }
		#endregion
		#region NoOps

		public async Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock) {
            var response = SubscriptionResponse<long>.Updated(recurRock.Id);
        }

		public async Task UnArchiveRock(ISession s, RockModel rock, bool v) {
            //Nothing
        }
        public async Task UndeleteRock(ISession s, RockModel rock) {
			//Nothing
		}

		#endregion

	}
}
