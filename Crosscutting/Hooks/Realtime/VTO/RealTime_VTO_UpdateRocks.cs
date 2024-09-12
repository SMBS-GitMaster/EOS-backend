using RadialReview.Utilities.Hooks;
using System;
using System.Linq;
using NHibernate;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.VTO;
using RadialReview.Models;
using RadialReview.Utilities.RealTime;

namespace RadialReview.Crosscutting.Hooks.Realtime {
	public class RealTime_VTO_UpdateRocks : IRockHook, IMeetingRockHook {

		public bool CanRunRemotely() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}
		public bool AbsorbErrors() {
			return false;
		}

		private async Task _DoUpdate(ISession s, long rockId, long? recurrenceId, bool allowDeleted, string connectionId, Func<long, L10Recurrence.L10Recurrence_Rocks, AngularUpdate> action) {
			var recurRocksQ = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.ForRock.Id == rockId);
			if (recurrenceId != null) {
				recurRocksQ = recurRocksQ.Where(x => x.L10Recurrence.Id == recurrenceId);
			}
			if (!allowDeleted) {
				recurRocksQ = recurRocksQ.Where(x => x.DeleteTime == null);
			}

			var recurRocks = recurRocksQ.List().ToList();

			await using (var rt = RealTimeUtility.Create(connectionId)) {



				foreach (var recurRock in recurRocks) {
					var vtoId = recurRock.L10Recurrence.VtoId;

					var qrId = s.QueryOver<QuarterlyRocksModel>().Where(x => x.Vto == vtoId).SingleOrDefault().Id;

					var updates = action(qrId, recurRock);
					var group = rt.UpdateVtos(vtoId);
					foreach (var u in updates.GetUpdates()) {
						group.Update(u);
					}
				}
			}
		}

		private async Task AddRockToVto(ISession s, long rockId, long? recurrenceId) {
			await _DoUpdate(s, rockId, recurrenceId, false, null, (qrId, recurRock) =>
				 new AngularUpdate() {
					new AngularQuarterlyRocks(qrId) {
						Rocks = AngularList.CreateFrom(AngularListType.Add, AngularVtoRock.Create(recurRock))
					}
				 }
			);
		}

		private async Task RemoveRockFromVto(ISession s, long rockId, long? recurrenceId) {
			await _DoUpdate(s, rockId, recurrenceId, true, null, (qrId, recurRock) =>
				  new AngularUpdate() {
					new AngularQuarterlyRocks(qrId) {
						Rocks = AngularList.CreateFrom(AngularListType.Remove, new AngularVtoRock(recurRock.Id))
					}
				  }
			);
		}

		private async Task UpdateRock(ISession s, long rockId, long? recurrenceId) {
			await _DoUpdate(s, rockId, recurrenceId, false, RealTimeHelpers.GetConnectionString(), (qrId, recurRock) =>
				 new AngularUpdate() {
					new AngularQuarterlyRocks(qrId) {
						Rocks = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, AngularVtoRock.Create(recurRock))
					}
				 }
			);
		}


		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
			await RemoveRockFromVto(s, rock.Id, null);
		}

		public async Task UnArchiveRock(ISession s, RockModel rock, bool v) {
			//Nothing to do...
		}
		public async Task UndeleteRock(ISession s, RockModel rock) {
			//Nothing
		}

		public async Task AttachRock(ISession s, UserOrganizationModel caller, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock) {
			if (recurRock.VtoRock) {
				await AddRockToVto(s, rock.Id, recurRock.L10Recurrence.Id);
			}
		}
		public async Task DetachRock(ISession s, RockModel rock, long recurrenceId, IMeetingRockHookUpdates updates) {
			await RemoveRockFromVto(s, rock.Id, recurrenceId);
		}

		public async Task CreateRock(ISession s, UserOrganizationModel caller, RockModel rock) {
			//Nothing to do
		}

		public async Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates) {
			await UpdateRock(s, rock.Id, null);
		}

		public async Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock) {
			if (recurRock.VtoRock && recurRock.DeleteTime == null) {
				await AddRockToVto(s, recurRock.ForRock.Id, recurRock.L10Recurrence.Id);
			} else {
				await RemoveRockFromVto(s, recurRock.ForRock.Id, recurRock.L10Recurrence.Id);
			}
		}

	}
}
