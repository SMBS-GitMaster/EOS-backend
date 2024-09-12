using RadialReview.Utilities.Hooks;
using System.Linq;
using NHibernate;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using System.Threading.Tasks;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Accessors;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Base;
using RadialReview.Models;

namespace RadialReview.Crosscutting.Hooks.Realtime.L10 {
	public class RealTime_L10_UpdateRocks : IRockHook, IMeetingRockHook {

		public bool CanRunRemotely() {
			return false;
		}
		public bool AbsorbErrors() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task AttachRock(ISession s, UserOrganizationModel caller, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock) {
			await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				rt.UpdateRecurrences(recurRock.L10Recurrence.Id).Update(rid => new AngularRock(recurRock.ForRock.Id) { VtoRock = recurRock.VtoRock });
				rt.UpdateRecurrences(recurRock.L10Recurrence.Id).Call("recalculatePercentage");

				var adminPerms = PermissionsUtility.CreateAdmin(s);
				var recurrenceId = recurRock.L10Recurrence.Id;
				var recur = s.Get<L10Recurrence>(recurrenceId);

				var current = L10Accessor._GetCurrentL10Meeting_Unsafe(s, recurrenceId, true, false, false);
				await using (var rt2 = RealTimeUtility.Create()) {
					var group = rt2.UpdateRecurrences(recurrenceId);

					if (current != null) {
						await _UpdateRocksInMeeting(s, rock, recurRock, adminPerms, recurrenceId, recur, current, group);
					} else {
						var recurRocks = L10Accessor.GetRocksForRecurrence(s, adminPerms, recurrenceId);
						string focus = null;
						if (recurRocks.Any() && recurRocks.Last().ForRock != null) {
							focus = "[data-rock='" + rock.Id + "'] input:visible:first";
						}

						group.Update(new AngularRecurrence(recurrenceId) {
							Rocks = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularRock(recurRock)),
						}
						);

						if (RealTimeHelpers.GetConnectionString() != null) {
							await using (var rt3 = RealTimeUtility.Create()) {
								var me = rt3.UpdateConnection(RealTimeHelpers.GetConnectionString());
								me.Update(new AngularRecurrence(recurrenceId) { Focus = focus });
							}
						}
					}
				}
			}
		}

		private static async Task _UpdateRocksInMeeting(ISession s, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock, PermissionsUtility adminPerms, long recurrenceId, L10Recurrence recur, L10Meeting current, RealTimeUtility.RecurrenceUpdater group) {
			await _UpdateL10MeetingRocks(s, adminPerms, recurrenceId, recur, current, group);

			//Update Angular
			var arecur = new AngularRecurrence(recurrenceId) {
				Rocks = AngularList.Create(AngularListType.ReplaceIfNewer, new[]{new AngularRock(recurRock){
							ForceOrder =int.MaxValue,
						}}),
			};
			group.Update(arecur);

			if (RealTimeHelpers.GetConnectionString() != null) {
				await using (var rt = RealTimeUtility.Create()) {
					var me = rt.UpdateConnection(RealTimeHelpers.GetConnectionString());
					me.Update(new AngularRecurrence(recurrenceId) {
						Focus = "[data-rock='" + rock.Id + "'] input:visible:first"
					});
				}
			}
		}

		private static async Task _UpdateL10MeetingRocks(ISession s, PermissionsUtility adminPerms, long recurrenceId, L10Recurrence recur, L10Meeting current, RealTimeUtility.RecurrenceUpdater group) {
			var rocksAndMilestones = L10Accessor.GetRocksForMeeting(s, adminPerms, recurrenceId, current.Id);
			var builder = "";
			if (!recur.CombineRocks && rocksAndMilestones.Where(x => x.Rock.VtoRock).Any()) {
				var vtoRocks = rocksAndMilestones.Select(x => x.Rock).Where(x => x.VtoRock).ToList();
				var crow = ViewUtility.RenderPartial("~/Views/L10/partial/CompanyRockGroup.cshtml", vtoRocks);
				string companyRocks = await crow.ExecuteAsync();
				builder += " <div class='company-rock-container'> " + companyRocks + " <hr/> </div> ";
			}

			//Update Weekly Meeting (L10) meeting
			var row = ViewUtility.RenderPartial("~/Views/L10/partial/RockGroup.cshtml", rocksAndMilestones.Select(x => x.Rock).ToList());
			builder = builder + (await row.ExecuteAsync());
			group.Call("updateRocks", builder);
		}

		public async Task DetachRock(ISession s, RockModel rock, long recurrenceId, IMeetingRockHookUpdates updates) {
			await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				var recur = s.Get<L10Recurrence>(recurrenceId);

				if (recur.MeetingInProgress != null) {
					var meeting = s.Get<L10Meeting>(recur.MeetingInProgress.Value);
					var group = rt.UpdateRecurrences(recurrenceId);
					await _UpdateL10MeetingRocks(s, PermissionsUtility.CreateAdmin(s), recurrenceId, recur, meeting, group);
				}

				rt.UpdateRecurrences(recurrenceId).Update(
					new AngularRecurrence(recurrenceId) {
						Rocks = AngularList.CreateFrom(AngularListType.ReplaceIfExists, new AngularRock(rock.Id) {
							Archived = true,
						})
					}
				);
			}
		}

		public async Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates) {
			await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				var rock_ids = RealTimeHelpers.GetRecurrenceRockData(s, rock.Id);
				var allRecurIds = rock_ids.RecurData.Select(x => x.RecurrenceId);

				var updater = rt.UpdateRecurrences(allRecurIds).Update(new AngularRock(rock, null));
				//Update Name
				if (updates.MessageChanged)
					updater.Call("updateRockName", rock.Id, rock.Name);

				//Update Due Date
				if (updates.DueDateChanged)
					updater.Call("updateRockDueDate", rock.Id, rock.DueDate.Value.ToJsMs());

				if (updates.AccountableUserChanged) { 
					updater.Call("updateRockOwner", rock.Id, rock.AccountableUser.Id, rock.AccountableUser.GetName(), rock.AccountableUser.ImageUrl(true, ImageSize._32));
                    updater.Call("moveRockToNewOwner", rock.Id, rock.AccountableUser.Id, rock.AccountableUser.GetName(), rock.AccountableUser.ImageUrl(true, ImageSize._32));
                  }

				//Update Completion
				if (updates.StatusChanged) {
					updater.Call("updateRockCompletion", rock.Id, rock.Completion.ToString());
					updater.Call("updateRockCompletion", 0, rock.Completion.ToString(), rock.Id);
				}
			}
		}
		public async Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock) {
			await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				rt.UpdateRecurrences(recurRock.L10Recurrence.Id)
					.Update(rid => new AngularRock(recurRock.ForRock.Id) {
						VtoRock = recurRock.VtoRock
					});
			}


			var adminPerms = PermissionsUtility.CreateAdmin(s);
			var rock = recurRock.ForRock;
			var recurrenceId = recurRock.L10Recurrence.Id;
			var recur = s.Get<L10Recurrence>(recurrenceId);

			var current = L10Accessor._GetCurrentL10Meeting_Unsafe(s, recurrenceId, true, false, false);
			await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				var group = rt.UpdateRecurrences(recurrenceId);
				if (current != null) {
					await _UpdateRocksInMeeting(s, rock, recurRock, adminPerms, recurrenceId, recur, current, group);
				}
			}

		}

		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
			//Nothing to do..
		}

		public async Task UnArchiveRock(ISession s, RockModel rock, bool v) {
			//Nothing to do...
		}
		public async Task UndeleteRock(ISession s, RockModel rock) {
			//Nothing
		}

		public async Task CreateRock(ISession s, UserOrganizationModel caller, RockModel rock) {
			//Nothing to do..
		}
	}
}
