using NHibernate;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Askables;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Realtime.AccountabilityChart {
	public class RealTimeUpdateRoles : ISimpleRoleHook {
		public bool AbsorbErrors() {
			return true;
		}

		public bool CanRunRemotely() {
			return false;
		}

		public async Task CreateRole(ISession s, long simpleRoleId) {
			//noop
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task UpdateRole(ISession s, long simpleRoleId, ISimpleRoleHookUpdates updates) {
			await using (var rt = RealTimeUtility.Create(/*SkipUser set below*/)) {
				var role = s.Get<SimpleRole>(simpleRoleId);
				var updater = rt.UpdateOrganization(role.OrgId);
				var any = false;
				var skip = true;
				if (updates.DeleteTimeChanged) {
					if (role.DeleteTime != null) {
						//role was deleted
						updater.Update(AngularRoleGroup.CreateForNode(role.NodeId, AngularList.CreateFrom(AngularListType.Remove, new AngularRole(simpleRoleId))));
					} else {
						//role was undeleted
						updater.Update(AngularRoleGroup.CreateForNode(role.NodeId, AngularList.CreateFrom(AngularListType.Add, new AngularRole(role))));
					}
					any = true;
					skip = false;
				}

				if (updates.OrderingChanged) {
					updater.Update(new AngularRole(role.Id) { Ordering = role.Ordering });
					any = true;
				}


				if (updates.NameChanged) {
					updater.Update(new AngularRole(role.Id) { Name = role.Name });
					any = true;
				}


				if (!any) {
					//role was updated
					updater.Update(new AngularRole(role));
					skip = false;
				}

				if (skip) {
					rt.SetSkipUser(RealTimeHelpers.GetConnectionString());
				}
			}
		}
	}
}
