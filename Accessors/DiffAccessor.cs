using NHibernate;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.NHibernate;
using System;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public class DiffAccessor {

		public static async Task<bool> SendDiff(IOuterSession s, PermissionsUtility perms, long callerId, Diff diff) {
			perms.Self(callerId);
			var caller = s.Get<UserOrganizationModel>(callerId);
			var type = diff.Type.ToLower();
			switch (type) {
				case "wb":
					await SendWhiteboardDiff(s, perms, caller.Id, caller.Organization.Id, diff);
					break;
				default:
					throw new NotImplementedException("Unhandled type:" + type);
			}

			return true;
		}

		private static async Task SendWhiteboardDiff(ISession s, PermissionsUtility perms, long callerId, long orgId, Diff diff) {
			await WhiteboardAccessor.SaveDiff(s, perms, callerId, orgId, diff);
		}
	}
}