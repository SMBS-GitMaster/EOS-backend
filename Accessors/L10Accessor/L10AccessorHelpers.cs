using System;
using System.Collections.Generic;
using RadialReview.Accessors.TodoIntegrations;
using RadialReview.Models;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using NHibernate;
using RadialReview.Models.Interfaces;
using System.Linq;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {

		#region Helpers
		public static string GetMeetingName(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Recurrence(recurrenceId);
					return s.Get<L10Recurrence>(recurrenceId).NotNull(x => x.Name);
				}
			}
		}
		public static List<AbstractTodoCreds> GetExternalLinksForRecurrence(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return ExternalTodoAccessor.GetExternalLinksForModel(s, PermissionsUtility.Create(s, caller), ForModel.Create<L10Recurrence>(recurrenceId));
				}
			}
		}

		public static bool _ProcessDeleted(ISession s, IDeletable item, bool? delete, double? deleteTimestamp = null) {
			if (delete != null) {
				if (delete == true && item.DeleteTime == null) {
                    var deleteTime = deleteTimestamp != null
                      ? DateTimeOffset.FromUnixTimeSeconds((long) deleteTimestamp).DateTime
                      : DateTime.UtcNow;
					item.DeleteTime = deleteTime;
					s.Update(item);
					return true;
				} else if (delete == false && item.DeleteTime != null) {
					item.DeleteTime = null;
					s.Update(item);
					return true;
				}
			}
			return false;
		}
		public static object GetModel_Unsafe(ISession s, string type, long id) {
			if (id <= 0)
				return null;

			switch (type.ToLower()) {
				case "measurablemodel":
					return s.Get<MeasurableModel>(id);
				case "todomodel":
					return s.Get<TodoModel>(id);
				case "issuemodel":
					return s.Get<IssueModel>(id);
			}
			return null;
		}

		public static string GetDefaultStartPage(L10Recurrence recurrence) {
			if (recurrence == null || recurrence._Pages == null) {
				return "nopage";
			}
			var page = recurrence._Pages.FirstOrDefault();
			if (page != null) {
				return "page-" + page.Id;
			} else {
				return "nopage";
			}
		}

		#endregion
	}
}