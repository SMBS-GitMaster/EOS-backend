using RadialReview.Utilities.Hooks;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.L10;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Users;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Base;
using System.Collections.Generic;

namespace RadialReview.Crosscutting.Hooks.Meeting {
	public class AttendeeHooks : IMeetingEvents {

    public bool CanRunRemotely() {
			return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public bool AbsorbErrors() {
			return false;
		}

		public async Task AddAttendee(ISession s, long recurrenceId, UserOrganizationModel user, L10Recurrence.L10Recurrence_Attendee attendee) {
			var auser = AngularUser.CreateUser(user);

			auser.Managing = true; /*This is really wrong, but the backend will catch any issues, need to make each user do a perms check... 
			hard to do when pushing an update.
			*/

			auser.CreateTime = attendee.CreateTime;

			await using (var rt = RealTimeUtility.Create()) {
				rt.UpdateRecurrences(recurrenceId).Update(new AngularRecurrence(recurrenceId) {
					Attendees = AngularList.CreateFrom(AngularListType.Add, auser)
				});
			}
		}

    public async Task EditAttendee(ISession s, long recurrenceId, UserOrganizationModel user, L10Recurrence.L10Recurrence_Attendee attendee)
    {
      //noop
    }

    public async Task RemoveAttendee(ISession s, long recurrenceId, long userId, List<L10Recurrence.L10Recurrence_Attendee> removedFromRecurrence, List<L10Meeting.L10Meeting_Attendee> removedFromMeeting) {
			await using (var rt = RealTimeUtility.Create()) {
				rt.UpdateRecurrences(recurrenceId).Update(new AngularRecurrence(recurrenceId) {
					Attendees = AngularList.CreateFrom(AngularListType.Remove, new AngularUser(userId))
				});
			}
		}


		#region noop
		public async Task ConcludeMeeting(ISession s, UserOrganizationModel user, L10Recurrence recur, L10Meeting meeting) {
			//noop
		}

		public async Task CreateRecurrence(ISession s, L10Recurrence recur) {
			//noop
		}

        public async Task UpdateRecurrence(ISession s, UserOrganizationModel caller, L10Recurrence recur)
        {
        //noop
        }

        public async Task DeleteMeeting(ISession s, L10Meeting meeting) {
			//noop
		}

		public async Task DeleteRecurrence(ISession s, L10Recurrence recur) {
			//noop
		}

		public async Task StartMeeting(ISession s , L10Recurrence recur, L10Meeting meeting) {
          //noop
        }

		public async Task UndeleteRecurrence(ISession s, L10Recurrence recur) {
			//noop
		}

		public async Task CurrentPageChanged(ISession s, long recurrenceId, L10Meeting meeting, string pageName, double now_ms, double baseMins)
		{
		//noop
		}

    public async Task UpdateCurrentMeetingInstance(ISession s, UserOrganizationModel caller, L10Recurrence recurrence)
    {
      //noop
    }

    public async Task CreatePage(ISession s, UserOrganizationModel caller, L10Recurrence.L10Recurrence_Page page)
    {
      // noop
    }

    public async Task UpdatePage(ISession s, UserOrganizationModel caller, L10Recurrence.L10Recurrence_Page page)
    {
      // noop
    }

    public async Task RemovePage(ISession s, UserOrganizationModel caller, L10Recurrence.L10Recurrence_Page page)
    {
      // noop
    }

    public async Task UpdateUserFeedback(ISession session, UserOrganizationModel user)
    {
      // noop
    }
    #endregion
  }
}
