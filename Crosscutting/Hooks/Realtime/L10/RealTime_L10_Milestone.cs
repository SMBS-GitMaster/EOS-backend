using RadialReview.Utilities.Hooks;
using NHibernate;
using System.Threading.Tasks;
using RadialReview.Hubs;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models;
using RadialReview.Models.Rocks;
using RadialReview.Models.Askables;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.Angular.Dashboard;

namespace RadialReview.Crosscutting.Hooks.Realtime.L10 {
	public class RealTime_L10_Milestone : IMilestoneHook {
		public bool CanRunRemotely() {
			return false;
		}
		public bool AbsorbErrors() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task CreateMilestone(ISession s, Milestone milestone) {

			var rock = s.Get<RockModel>(milestone.RockId);
			await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString()))
			{
				var group = rt.UpdateGroup(RealTimeHub.Keys.UserId(rock.ForUserId));
				group.Update(new ListDataVM(rock.ForUserId)
				{
					Milestones = AngularList.CreateFrom(AngularListType.Add, new AngularTodo(milestone, rock.AccountableUser, rock.Name))
				});
			}

		}

		public async Task UpdateMilestone(ISession s, UserOrganizationModel caller, Milestone milestone, IMilestoneHookUpdates updates) {

			var rock = s.Get<RockModel>(milestone.RockId);
			await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				var group = rt.UpdateGroup(RealTimeHub.Keys.UserId(rock.ForUserId));
				if (updates.IsDeleted) {
					var update1 = new AngularRecurrence(-2);
					update1.Milestones = AngularList.CreateFrom(AngularListType.Remove, new AngularTodo(milestone, rock.AccountableUser));
					group.Update(update1);
				} else
					group.Update( new AngularTodo(milestone, rock.AccountableUser, rock.Name));

			}
		}
	}
}