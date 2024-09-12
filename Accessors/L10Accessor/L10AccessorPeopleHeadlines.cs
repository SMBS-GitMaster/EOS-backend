using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using NHibernate;
using RadialReview.Utilities.DataTypes;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
//using System.Web.WebPages.Html;
using RadialReview.Utilities.RealTime;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Middleware.Services.NotesProvider;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {

		#region PeopleHeadlines		
		public static List<PeopleHeadline> GetHeadlinesForMeeting(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetHeadlinesForMeeting(s, perms, recurrenceId);
				}
			}
		}

    public static List<PeopleHeadline> GetHeadlinesForMeeting(ISession s, PermissionsUtility perms, long recurrenceId, bool includeClosed = false) {
			perms.ViewL10Recurrence(recurrenceId);

			var foundQ = s.QueryOver<PeopleHeadline>().Where(x => x.DeleteTime == null && x.RecurrenceId == recurrenceId);
			if (!includeClosed)
				foundQ = foundQ.Where(x => x.CloseTime == null);

			var found = foundQ.Fetch(x => x.Owner).Eager
								.Fetch(x => x.About).Eager
								.List().ToList();

			foreach (var f in found) {
				if (f.Owner != null) {
					var a = f.Owner.GetName();
					var b = f.Owner.ImageUrl(true, ImageSize._32);
				}
				if (f.About != null) {
					var a = f.About.GetName();
					var b = f.About.GetImageUrl();
				}
			}
			return found;
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public static async Task RemoveHeadline(ISession s, PermissionsUtility perm, RealTimeUtility rt, long headlineId, DateTime closeTime) {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously


			perm.ViewHeadline(headlineId);

			var r = s.Get<PeopleHeadline>(headlineId);

			if (r.CloseTime != null)
				throw new PermissionsException("Headline already removed.");

			perm.EditL10Recurrence(r.RecurrenceId);

			r.CloseTime = closeTime;
			s.Update(r);

			await HooksRegistry.Each<IHeadlineHook>((ses, x) => x.ArchiveHeadline(ses, r));
		}

    public static List<PeopleHeadline> GetAllHeadlinesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, bool includeClosed, DateRange range, bool includeArchivedHeadlines = true) {
            perms.ViewL10Recurrence(recurrenceId);

            var headlineListQ = s.QueryOver<PeopleHeadline>().Where(x => x.DeleteTime == null && x.RecurrenceId == recurrenceId);

            if(includeArchivedHeadlines) { 
                  if((range != null && includeClosed)) {
				      var st = range.StartTime.AddDays(-1);
				      var et = range.EndTime.AddDays(1);
				      headlineListQ = headlineListQ.Where(x => x.CloseTime == null || (x.CloseTime >= st && x.CloseTime <= et));
			      }
            } else {
              headlineListQ = headlineListQ.Where(x => x.CloseTime == null);
            }
			var headlineList = headlineListQ.List().ToList();
			foreach (var t in headlineList) {
				if (t.About != null) {
					var a = t.About.GetName();
					var b = t.About.GetImageUrl();
				}
				if (t.Owner != null) {
					var a = t.Owner.GetName();
					var b = t.Owner.GetImageUrl();
				}
			}
			return headlineList;
		}
		#endregion
	}
}