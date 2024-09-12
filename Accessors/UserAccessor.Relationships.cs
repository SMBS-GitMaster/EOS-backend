using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;


namespace RadialReview.Accessors {
	public partial class UserAccessor : BaseAccessor {
		public static List<UserOrganizationModel> GetPeers(UserOrganizationModel caller, long forId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetPeers(s.ToQueryProvider(true), perms, caller, forId);
				}
			}
		}

		public static List<UserOrganizationModel> GetPeers(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, long forId) {
			perms.ViewUserOrganization(forId, false);
			var forUser = s.Get<UserOrganizationModel>(forId);
			if (forUser.ManagingUsers.All(x => x.DeleteTime != null)) {
				return forUser.ManagedBy.ToListAlive().Select(x => x.Manager).SelectMany(x => x.ManagingUsers.ToListAlive().Select(y => y.Subordinate)).Where(x => x.Id != forId).ToList();
			}

			return new List<UserOrganizationModel>();
		}

		public static List<UserOrganizationModel> GetManagers(UserOrganizationModel caller, long forUserId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetManagers(s.ToQueryProvider(true), perms, caller, forUserId);
				}
			}
		}

		public static List<UserOrganizationModel> GetManagers(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, long forUserId) {
			perms.ViewUserOrganization(forUserId, false);
			var forUser = s.Get<UserOrganizationModel>(forUserId);
			return forUser.ManagedBy.ToListAlive().Select(x => x.Manager).Where(x => x.Id != forUserId).ToList();
		}

		public static List<UserOrganizationModel> GetDirectSubordinates(UserOrganizationModel caller, long forId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetDirectSubordinates(s.ToQueryProvider(true), perms, forId);
				}
			}
		}

		public static List<UserOrganizationModel> GetDirectSubordinates(AbstractQuery s, PermissionsUtility perms, long forId) {
			perms.ViewUserOrganization(forId, false);
			var forUser = s.Get<UserOrganizationModel>(forId);
			return forUser.ManagingUsers.ToListAlive().Select(x => x.Subordinate).Where(x => x.Id != forId).ToListAlive();
		}


		[Obsolete("This is old. Only used for testing.")]
		public static void AddManager(ISession s, PermissionsUtility perms, long userId, long managerId, DateTime now, bool ignoreCircular = false) {
			perms.ManagesUserOrganization(userId, true).ManagesUserOrganization(managerId, false);
			AddMangerUnsafe(s, perms.GetCaller(), userId, managerId, now, ignoreCircular);
		}

		[Obsolete("This is old. Only used for testing.")]
		public static void AddManager(UserOrganizationModel caller, long userId, long managerId, DateTime now) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					AddManager(s, perms, userId, managerId, now);
					tx.Commit();
					s.Flush();
				}
			}
		}

		[Obsolete("This is old. Only used for testing.")]
		private static void AddMangerUnsafe(ISession s, UserOrganizationModel caller, long userId, long managerId, DateTime now, bool ignoreCircular = false) {
			var user = s.Get<UserOrganizationModel>(userId);
			var manager = s.Get<UserOrganizationModel>(managerId);
			var managerNull = user.ManagedBy.ToListAlive().Where(x => x.ManagerId == managerId).FirstOrDefault();
			if (managerNull != null) {
				throw new PermissionsException(manager.GetName() + " is already a " + Config.ManagerName() + " for this user.");
			}

			if (!manager.IsManager()) {
				throw new PermissionsException(manager.GetName() + " is not a " + Config.ManagerName() + ".");
			}

			user.ManagedBy.Add(new ManagerDuration(managerId, userId, caller.Id) { CreateTime = now, Manager = manager, Subordinate = user });
			s.Update(user);
			user.UpdateCache(s);
		}


		#region Deleted

		[Obsolete("Do not use. It only for fixing bugs.")]
		public static void RemoveManager(ISession s, PermissionsUtility perms, UserOrganizationModel caller, long managerDurationId, DateTime now) {
			var managerDuration = s.Get<ManagerDuration>(managerDurationId);
			perms.ManagesUserOrganization(managerDuration.SubordinateId, true).ManagesUserOrganization(managerDuration.ManagerId, false);
			RemoveMangerUnsafe(s, caller, managerDuration, now);
		}

		[Obsolete("Do not use. It only for fixing bugs.")]
		private static void RemoveMangerUnsafe(ISession s, UserOrganizationModel caller, ManagerDuration managerDuration, DateTime now) {
			managerDuration.DeletedBy = caller.Id;
			managerDuration.DeleteTime = now;
			s.Update(managerDuration);
			managerDuration.Subordinate.UpdateCache(s);
			managerDuration.Manager.UpdateCache(s);
		}

		[Obsolete("Do not use. It only for fixing bugs.")]
		public static void RemoveManager(UserOrganizationModel caller, long managerDurationId, DateTime now) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					RemoveManager(s, perms, caller, managerDurationId, now);
					tx.Commit();
					s.Flush();
				}
			}
		}
		#endregion

	}
}
