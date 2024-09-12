using NHibernate;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Positions;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public partial class AccountabilityAccessor : BaseAccessor {



		public static async Task SetPosition(UserOrganizationModel caller, long seatId, string positionName) {
			using (var usersToUpdate = new UserCacheUpdater()) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						await using (var rt = RealTimeUtility.Create()) {
							var perms = PermissionsUtility.Create(s, caller);
							SetPosition(s, perms, rt, seatId, positionName, usersToUpdate);

							tx.Commit();
							s.Flush();
						}
					}
				}
			}
		}
		public static void SetPosition(ISession s, PermissionsUtility perms, RealTimeUtility rt, long nodeId, string positionName, UserCacheUpdater usersToUpdate) {
			perms.ManagesAccountabilityNodeOrSelf(nodeId);

			var now = DateTime.UtcNow;
			UpdatePosition_Unsafe(s, rt, perms, nodeId, positionName, now, usersToUpdate);

		}

		[Todo]
		public static async void UpdatePosition_Unsafe(ISession s, RealTimeUtility rt, PermissionsUtility perms, long nodeId, string positionName, DateTime now, UserCacheUpdater usersToUpdate, bool skipAddPosition = false, bool isDeleteSeat = false) {
			var node = s.Get<AccountabilityNode>(nodeId);
			var arg = node.GetAccountabilityRolesGroup();
			var updater = rt.UpdateOrganization(arg.OrganizationId);

			string newPosition = null;

			if (arg.PositionName != positionName) {
				//Delete old position
				if (!string.IsNullOrEmpty(arg.PositionName)) {
					foreach (var nn in node.GetUsers(s)) {
						var pd = s.QueryOver<PositionDurationModel>().Where(x => x.DeleteTime == null && x.PositionName == arg.PositionName && x.UserId == nn.Id).Take(1).SingleOrDefault();
						if (pd != null) {
							pd.DeleteTime = now;
							pd.DeletedBy = perms.GetCaller().Id;
							s.Update(pd);
							usersToUpdate.Add(nn.Id);
						}
					}
					newPosition = positionName == null ? "" : null;
				}

				//Add new position
				if (!string.IsNullOrEmpty(positionName)) {
					arg.PositionName = positionName;

					if (!skipAddPosition) {
						foreach (var nn in node.GetUsers(s)) {
							var pd = new PositionDurationModel() {
								UserId = nn.Id,
								CreateTime = now,
								PositionName = positionName,
								PromotedBy = perms.GetCaller().Id,
								OrganizationId = node.OrganizationId
							};
							s.Save(pd);
							usersToUpdate.Add(nn.Id);
						}
					}


					newPosition = positionName;

				}

				if (newPosition != null) {
					updater.Update(new AngularPosition(arg.Id) {
						Name = newPosition
					});
				}

				arg.PositionName = positionName;
        node.AccountabilityRolesGroup.PositionName = positionName;
				s.Update(arg);

        var nodeUpdates = new IOrgChartSeatHookUpdates()
        {
          PositionTitle = true
        };

        if(!isDeleteSeat)
        {
          await HooksRegistry.Each<IOrgChartSeatHook>((ses, x) => x.UpdateOrgChartSeat(ses, perms.GetCaller(), node, nodeUpdates));
        }
      }
    }


		public static List<AngularPosition> GetPositionsForUser(UserOrganizationModel caller, long userId, bool includeUnnamed) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewUserOrganization(userId, false);
					return GetPositionsForUser_Unsafe(s, userId, includeUnnamed);

				}
			}
		}

		public static List<AngularPosition> GetPositionsForUser_Unsafe(ISession s, long userId, bool includeUnnamed) {
			var nodesForUser = DeepAccessor.Unsafe.Criterions.SelectNodeIdsGivenUser_Unsafe(userId);
			AccountabilityRolesGroup argAlias = null;
			return s.QueryOver<AccountabilityNode>()
						.JoinAlias(x => x.AccountabilityRolesGroup, () => argAlias)
						.Where(x => argAlias.DeleteTime == null)
						.WithSubquery.WhereProperty(x => x.Id).In(nodesForUser)
						.Select(x => argAlias.Id, x => argAlias.PositionName)
						.List<object[]>()
						.Select(x => new AngularPosition((long)x[0], (string)x[1]))
						.Where(x => includeUnnamed || !string.IsNullOrEmpty(x.Name))
						.ToList();
		}
	}
}
