using System;
using System.Linq;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using System.Threading.Tasks;
using static RadialReview.GraphQL.Models.IssueQueryModel.Associations;
using RadialReview.Repositories;
using RadialReview.Exceptions.MeetingExceptions;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
//using System.Web.WebPages.Html;

namespace RadialReview.Accessors {
  public partial class L10Accessor : BaseAccessor {



    #region Pages


		public static L10Recurrence.L10Recurrence_Page GetPageInRecurrence(UserOrganizationModel caller, long pageId, long recurrenceId) {
#pragma warning disable CS0618 // Type or member is obsolete
			var page = GetPage(caller, pageId);
#pragma warning restore CS0618 // Type or member is obsolete
			if (page.L10RecurrenceId != recurrenceId)
				throw new PermissionsException("Page does not exist.");
			return page;
		}

		[Obsolete("Should you use GetPageInRecurrence?")]
		public static L10Recurrence.L10Recurrence_Page GetPage(UserOrganizationModel caller, long pageId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var page = GetPage(s, perms, pageId);
					return page;
				}
			}
		}
		[Obsolete("Should you use GetPageInRecurrence?")]
		public static L10Recurrence.L10Recurrence_Page GetPage(ISession s, PermissionsUtility perms, long pageId) {
			var page = s.Get<L10Recurrence.L10Recurrence_Page>(pageId);
			perms.ViewL10Recurrence(page.L10Recurrence.Id);
			if (page.DeleteTime != null)
				throw new PermissionsException("Page does not exist.");

			return page;
		}


		public static async Task<L10Recurrence.L10Recurrence_Page> EditOrCreatePage(UserOrganizationModel caller, L10Recurrence.L10Recurrence_Page page, bool isModeChangeOnly, int? checkInType = null, string iceBreaker = null, bool? isAttendanceVisible = null, bool? concludeMeeting = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					    var (existingPage, editOrCreate) = EditOrCreatePage(s, perms, page, isModeChangeOnly, checkInType, iceBreaker, isAttendanceVisible, concludeMeeting: concludeMeeting);

					tx.Commit();
					s.Flush();

                    switch (editOrCreate)
                    {
                        case EditOrCreate.Edit:
                        await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.UpdatePage(ses, caller, existingPage));
                        break;

                        case EditOrCreate.Create:
                        await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.CreatePage(ses, caller, existingPage));
                        break;
                    }

					return existingPage;
				}
			}
		}

        public enum EditOrCreate
        {
            Edit,
            Create,
        }

    public static void verifyEditViewRecurrencePermissions(PermissionsUtility perms, long recurrenceId)
    {
      try
      {
        perms.ViewL10Recurrence(recurrenceId);
      }
      catch (PermissionsException ex)
      {
        perms.EditL10Recurrence(recurrenceId);
      }
    }
    public static (L10Recurrence.L10Recurrence_Page, EditOrCreate) EditOrCreatePage(ISession s, PermissionsUtility perms, L10Recurrence.L10Recurrence_Page page, bool isModeChangeOnly, int? checkInType = null, string iceBreaker = null, bool? isAttendanceVisible = null, bool? concludeMeeting = false) {
			var existingPage = s.Get<L10Recurrence.L10Recurrence_Page>(page.Id);
      		var action = EditOrCreate.Edit;

			if (existingPage == null) {
				var ordering = s.QueryOver<L10Recurrence.L10Recurrence_Page>()
									.Where(x => x.DeleteTime == null && x.L10RecurrenceId == page.L10RecurrenceId)
									.RowCount();
        		action = EditOrCreate.Create;
				existingPage = new L10Recurrence.L10Recurrence_Page() {
					L10RecurrenceId = page.L10RecurrenceId,
					_Ordering = ordering
				};
    		}

			if (isModeChangeOnly) {
				perms.ViewL10Recurrence(existingPage.L10RecurrenceId);
			} else if(!concludeMeeting.GetValueOrDefault())
      {
        long? leaderId = 0;

        try
        {
          leaderId = GetMeetingLeader(perms.GetCaller(), existingPage.L10RecurrenceId).Result;
        } catch(AggregateException ae) when (ae.InnerException is MeetingException) { } // meeting is not running

        if (perms.GetCaller().Id == leaderId)
          perms.ViewL10Recurrence(existingPage.L10RecurrenceId);
        else
          perms.AdminL10Recurrence(existingPage.L10RecurrenceId);
      } else
      {
        verifyEditViewRecurrencePermissions(perms, existingPage.L10RecurrenceId);
      }

			if (existingPage.L10RecurrenceId != page.L10RecurrenceId)
				throw new PermissionsException("RecurrenceIds do not match");

			existingPage.PageType = page.PageType;
			existingPage.Minutes = page.Minutes;
			existingPage.Title = page.Title;
			existingPage.Subheading = page.Subheading;
			existingPage.DeleteTime = page.DeleteTime;
			existingPage.Url = page.Url;
			existingPage._SummaryJson = page._SummaryJson ?? existingPage._SummaryJson;
      existingPage.TimeLastPaused = page.TimeLastPaused;
      existingPage.TimeLastStarted = page.TimeLastStarted;
      existingPage.TimePreviouslySpentS = page.TimePreviouslySpentS;
      existingPage.TimeSpentPausedS = page.TimeSpentPausedS;

      if (checkInType != null) existingPage.CheckInType = checkInType.Value;
      if (iceBreaker != null) existingPage.IceBreaker = iceBreaker;
      if (isAttendanceVisible != null) existingPage.IsAttendanceVisible = isAttendanceVisible.Value;

			s.SaveOrUpdate(existingPage);
			return (existingPage, action);
		}

		/*public static string GetPageType_Unsafe(ISession s, string pageName) {
			long pageId;
			if (pageName!=null && long.TryParse(pageName.SubstringAfter("-"), out pageId)) {

				var page = s.Get<L10Recurrence.L10Recurrence_Page>(pageId);
				if (page!=null && page.PageTypeStr!=null)
					return page.PageTypeStr.ToLower();
			}
			return (pageName??"").ToLower();
		}
		*/

		public static async Task ReorderPage(UserOrganizationModel caller, long pageId, int oldOrder, int newOrder) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var found = s.Get<L10Recurrence.L10Recurrence_Page>(pageId);
					PermissionsUtility.Create(s, caller).AdminL10Recurrence(found.L10RecurrenceId);

					var items = s.QueryOver<L10Recurrence.L10Recurrence_Page>().Where(x => x.DeleteTime == null && x.L10RecurrenceId == found.L10RecurrenceId).List().ToList();

					Reordering.CreateRecurrence(items, pageId, found.L10RecurrenceId, oldOrder, newOrder, x => x._Ordering, x => x.Id)
							  .ApplyReorder(s);

                    tx.Commit();
					s.Flush();

                    await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.UpdateRecurrence(s, caller, found.L10Recurrence));
				}
			}
		}

		#endregion
	}
}
