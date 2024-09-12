using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace RadialReview.Accessors {
	public partial class UserAccessor : BaseAccessor {


		public static List<long> WasAliveAt(ISession s, List<long> userOrgIds, DateTime time) {
			return s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(userOrgIds).Where(x => (x.CreateTime <= time) && (x.DeleteTime == null || time <= x.DeleteTime)).Select(x => x.Id).List<long>().ToList();
		}

		public static List<String> SideEffectRemove(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).RemoveUser(userId);
					var user = s.Get<UserOrganizationModel>(userId);
					var warnings = new List<String>();
					//managed teams
					var managedTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.ManagedBy == userId && x.DeleteTime == null).List().ToList();
					foreach (var m in managedTeams) {
						if (m.Type != TeamType.Subordinates) {
							warnings.Add("The team, " + m.GetName() + " is managed by" + user.GetFirstName() + ". You will be promoted to " + Config.ManagerName() + " of this team.");
						}
					}

					var subordinates = s.QueryOver<ManagerDuration>().Where(x => x.ManagerId == userId && x.DeleteTime == null).List().ToList();
					foreach (var subordinate in subordinates) {
						warnings.Add(user.GetFirstName() + " manages " + subordinate.Subordinate.GetName() + ".");
					}

					return warnings;
				}
			}
		}

		private static void AddSettings(IdentityResult result, UserModel user) {
			if (result.Succeeded) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var settings = new UserStyleSettings() { Id = user.Id, ShowScorecardColors = true };
						s.Save(settings);
						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static void SetHints(UserModel caller, bool turnedOn) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var user = s.Get<UserModel>(caller.Id);
					user.Hints = turnedOn;
					s.Update(user);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task UpdateUserWhaleFlag(UserOrganizationModel caller, long userId, bool whaleIO) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.EditUserModel(userId);
					var user = s.Get<UserOrganizationModel>(userId);
					var userLookup = s.QueryOver<UserLookup>().Where(x => x.UserId == userId).List().First();
					user.EnableWhale = whaleIO;
					userLookup.EnableWhale = whaleIO;
					s.Update(user);
					user.UpdateCache(s);
					tx.Commit();
					s.Flush();
				}
			}
		}



	}
}
