using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.Variables;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Meeting {
	public class FirstMeetingInjectedDataHooks : IMeetingEvents {
		public HookPriority GetHookPriority() {
			return HookPriority.Low;
		}
		public bool AbsorbErrors() {
			return true;
		}
		public bool CanRunRemotely() {
			return false;
		}


		public async Task StartMeeting(ISession s, L10Recurrence recur, L10Meeting meeting) {
			var r = s.Get<L10Recurrence>(recur.Id);
			//Check if first meeting
			if (recur.IsFirstMeeting && !meeting.Preview) {/*we want recur.IsFirstMeeting not r.IsFirstMeeting*/
				r.IsFirstMeeting = false;
				s.Update(r);
			}
		}

		#region noop
		public async Task AddAttendee(ISession s, long recurrenceId, UserOrganizationModel user, L10Recurrence.L10Recurrence_Attendee attendee) {
			//noop
		}

        public async Task EditAttendee(ISession s, long recurrenceId, UserOrganizationModel user, L10Recurrence.L10Recurrence_Attendee attendee)
        {
        //noop
        }

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
		public async Task RemoveAttendee(ISession s, long recurrenceId, long userId, List<L10Recurrence.L10Recurrence_Attendee> removedFromRecurrence, List<L10Meeting.L10Meeting_Attendee> removedFromMeeting) {
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
