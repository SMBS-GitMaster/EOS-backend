using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using System;
using RadialReview.Utilities.Query;

namespace RadialReview.Accessors {
	public class NexusAccessor : BaseAccessor {

		public static void Execute(NexusModel nexus) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					nexus = s.Get<NexusModel>(nexus.Id);
					nexus.DateExecuted = DateTime.UtcNow;
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static bool IsCorrectUser(UserOrganizationModel caller, NexusModel nexus) {
			if (caller.Id == nexus.ForUserId)
				return true;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var nexUsers = s.Get<UserOrganizationModel>(nexus.ForUserId);
					return caller.User.Id == nexUsers.User.Id;
				}
			}
		}

		public static NexusModel Put(NexusModel model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					model = Put(s.ToUpdateProvider(), model);
					tx.Commit();
					s.Flush();
				}
			}
			return model;
		}

		public static NexusModel Put(AbstractUpdate s, NexusModel model) {
			s.Save(model);
			return model;
		}

		public static NexusModel Get(String id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var found = s.Get<NexusModel>(id);
					if (found == null)
						throw new PermissionsException("The request was not found.");
					if (found.DeleteTime != null && DateTime.UtcNow > found.DeleteTime) {
						var message = "The request has expired.";
						if (found.ActionCode == NexusActions.ResetPassword) {
							message += " You can only use this password reset code once.";
						}
						throw new PermissionsException(message);
					}
					return found;
				}
			}
		}
	}
}