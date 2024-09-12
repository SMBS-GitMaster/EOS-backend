using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public partial class L10Accessor {
		public static async Task<string> GetOrCreateWhiteboardForL10Page(UserOrganizationModel caller, long recurrenceId, long pageId) {
			string whiteboardId;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					//Must be here
					perms.ViewL10Recurrence(recurrenceId);
					perms.ViewL10Page(pageId);

					var recur = s.Get<L10Recurrence>(recurrenceId);
					var page = s.Get<L10Recurrence.L10Recurrence_Page>(pageId);
					if (page.WhiteboardId == null) {
						var wb = await WhiteboardAccessor.CreateWhiteboard(s, perms, null, recur.Organization.Id, PermTiny.InheritedFromL10Recurrence(recurrenceId, true, true, true));
						page.WhiteboardId = wb.LookupId;
						s.Update(page);
						tx.Commit();
						s.Flush();
					}
					whiteboardId = page.WhiteboardId;

				}
			}
			return whiteboardId;
		}

		public static async Task<string> GetOrCreateWhiteboardForL10(UserOrganizationModel caller, long recurrenceId) {
			string whiteboardId;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					//Must be here
					perms.ViewL10Recurrence(recurrenceId);

					var recur = s.Get<L10Recurrence>(recurrenceId);
					if (recur.WhiteboardId == null) {
						var wb = await WhiteboardAccessor.CreateWhiteboard(s, perms, recur.Name	+" Whiteboard", recur.Organization.Id, PermTiny.InheritedFromL10Recurrence(recurrenceId, true, true, true));
						recur.WhiteboardId = wb.LookupId;
						s.Update(recur);
						tx.Commit();
						s.Flush();
					}
					whiteboardId = recur.WhiteboardId;

				}
			}
			return whiteboardId;
		}
	}
}