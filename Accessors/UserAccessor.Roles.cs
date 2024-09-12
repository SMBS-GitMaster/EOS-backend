using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ISession = NHibernate.ISession;
using RadialReview.Middleware.Request.HttpContextExtensions;
using RadialReview.Models.Admin;

namespace RadialReview.Accessors {
	public partial class UserAccessor : BaseAccessor {

		[Obsolete("Note: it's a bad idea to send the HttpContext to an accessor")]
		public static void ChangeRole(HttpContext ctx, UserModel caller, UserOrganizationModel callerUserOrg, long roleId, AdminAccessViewModel audit = null) {
			try {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						caller = s.Get<UserModel>(caller.Id);
						var myUserOrganizations = caller.UserOrganizationIds;
						var isAdmin = caller != null && ((caller.IsRadialAdmin) || (callerUserOrg != null && callerUserOrg.IsRadialAdmin));
						var requestedOrg = s.Get<UserOrganizationModel>(roleId).Organization;
						var recordAudit = new Action(() => s.Save(audit.ToDatabaseModel(caller.Id)));
						var canChange = Unsafe.CanChangeToRole(roleId, myUserOrganizations, requestedOrg.Id, requestedOrg.AccountType, Config.GetDisallowedOrgIds(s), isAdmin, audit, recordAudit);
						if (!canChange.Allowed) {
							throw new PermissionsException(canChange.Message);
						}

						caller.CurrentRole = roleId;
						s.Update(caller);
						tx.Commit();
						s.Flush();
					}
				}
			} finally {
				try {
					ctx.ClearUserOrganizationFromCache();
				} catch (Exception e) {
				}
			}
		}

		public static List<UserRole> GetUserRolesAtOrganization(UserOrganizationModel caller, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetUserRolesAtOrganization(s, perms, orgId);
				}
			}
		}

		public static List<UserRole> GetUserRolesAtOrganization(ISession s, PermissionsUtility perms, long orgId) {
			perms.ViewOrganization(orgId);
			return s.QueryOver<UserRole>().Where(x => x.DeleteTime == null && x.OrgId == orgId).List().ToList();
		}

		public static async Task SetRole(UserOrganizationModel caller, long userId, UserRoleType type, bool enabled) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					if (enabled) {
						await AddRole(s, perms, userId, type);
					} else {
						await RemoveRole(s, perms, userId, type);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task RemoveRole(UserOrganizationModel caller, long userId, UserRoleType type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					await RemoveRole(s, perms, userId, type);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task AddRole(UserOrganizationModel caller, long userId, UserRoleType type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					await AddRole(s, perms, userId, type);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task AddRole(ISession s, PermissionsUtility perms, long userId, UserRoleType type) {
			perms.ViewUserOrganization(userId, false);
			var any = s.QueryOver<UserRole>().Where(x => x.UserId == userId && type == x.RoleType && x.DeleteTime == null).RowCount();
			var user = s.Get<UserOrganizationModel>(userId);
			if (any == 0) {
				s.Save(new UserRole() { OrgId = user.Organization.Id, RoleType = type, UserId = userId, });
				await HooksRegistry.Each<IUserRoleHook>((ses, x) => x.AddRole(ses, userId, type));
			}
		}

		public static async Task RemoveRole(ISession s, PermissionsUtility perms, long userId, UserRoleType type) {
			perms.ViewUserOrganization(userId, false);
			var any = s.QueryOver<UserRole>().Where(x => x.UserId == userId && type == x.RoleType && x.DeleteTime == null).List().ToList();
			var user = s.Get<UserOrganizationModel>(userId);
			if (any.Count > 0) {
				foreach (var a in any) {
					a.DeleteTime = DateTime.UtcNow;
					s.Update(a);
				}
				await HooksRegistry.Each<IUserRoleHook>((ses, x) => x.RemoveRole(ses, userId, type));
			}
		}

	}
}
