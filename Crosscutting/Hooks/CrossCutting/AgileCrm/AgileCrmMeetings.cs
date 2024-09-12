using RadialReview.Utilities.Hooks;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.L10;
using System.Threading.Tasks;
using RadialReview.Utilities;
using static RadialReview.Utilities.Config;
using System.Collections.Generic;

namespace RadialReview.Hooks.CrossCutting.AgileCrm {
	public class AgileCrmMeetings : IMeetingEvents {
		public bool CanRunRemotely() {
			return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.Lowest;
		}
		public bool AbsorbErrors() {
			return true;
		}

		public AgileCrmConfig Configs { get; protected set; }
		public AgileCrmConnector Connector { get; protected set; }

		public AgileCrmMeetings() {
			Configs = Config.GetAgileCrmConfig();
			Connector = new AgileCrmConnector(Configs);
		}

		public async Task CreateRecurrence(ISession s, L10Recurrence recur) {
		}
		public async Task StartMeeting(ISession s, L10Recurrence recur, L10Meeting meeting) {
			var pid = s.Get<OrganizationModel>(meeting.OrganizationId);
			if (pid != null && pid.AgileOrganizationId != null) {
				await Connector.TagsAsync("AMeetingStarted", pid.AgileOrganizationId.Value);
			}
		}
		public async Task AddAttendee(ISession s, long recurrenceId, UserOrganizationModel user, L10Recurrence.L10Recurrence_Attendee attendee) {
			if (user.AgileUserId != null && user.AgileUserId != 0) {
				await Connector.TagsAsync("AttachedToMeeting", user.AgileUserId.Value);
			}
		}

		#region Noop
		public async Task ConcludeMeeting(ISession s, UserOrganizationModel user, L10Recurrence recur, L10Meeting meeting) {
			//Noop
		}
		public async Task DeleteMeeting(ISession s, L10Meeting meeting) {
			//Noop
		}

		public async Task DeleteRecurrence(ISession s, L10Recurrence recur) {
			//Noop
		}

		public async Task RemoveAttendee(ISession s, long recurrenceId, long userId, List<L10Recurrence.L10Recurrence_Attendee> removedFromRecurrence, List<L10Meeting.L10Meeting_Attendee> removedFromMeeting) {
			//Noop
		}

		public async Task UndeleteRecurrence(ISession s, L10Recurrence recur) {
			//Noop
		}

		public async Task CurrentPageChanged(ISession s, long recurrenceId, L10Meeting meeting, string pageName, double now_ms, double baseMins)
		{
		//noop
		}

    public async Task EditAttendee(ISession s, long recurrenceId, UserOrganizationModel user, L10Recurrence.L10Recurrence_Attendee attendee)
    {
      //noop
    }

    public async Task UpdateRecurrence(ISession s, UserOrganizationModel caller, L10Recurrence recur)
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
