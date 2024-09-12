using DocumentFormat.OpenXml.Office2010.ExcelAc;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.L10;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RadialReview.Utilities.Hooks
{
  public interface IMeetingEvents : IHook
  {
    Task CreateRecurrence(ISession s, L10Recurrence recur);
    Task UpdateRecurrence(ISession s, UserOrganizationModel caller, L10Recurrence recur);
    Task DeleteRecurrence(ISession s, L10Recurrence recur);
    Task UndeleteRecurrence(ISession s, L10Recurrence recur);
    Task StartMeeting(ISession s, L10Recurrence recur, L10Meeting meeting);
    Task ConcludeMeeting(ISession s, UserOrganizationModel user, L10Recurrence recur, L10Meeting meeting);

    Task CurrentPageChanged(ISession s, long reccurrenceId, L10Meeting meeting, string pageName, double now_ms, double baseMins);

    Task AddAttendee(ISession s, long recurrenceId, UserOrganizationModel user, L10Recurrence.L10Recurrence_Attendee attendee);
    Task EditAttendee(ISession s, long recurrenceId, UserOrganizationModel user, L10Recurrence.L10Recurrence_Attendee attendee);
    Task RemoveAttendee(ISession s, long recurrenceId, long userId, List<L10Recurrence.L10Recurrence_Attendee> removedFromRecurrence, List<L10Meeting.L10Meeting_Attendee> removedFromMeeting);
    Task UpdateUserFeedback(ISession session, UserOrganizationModel user);

    Task UpdateCurrentMeetingInstance(ISession s, UserOrganizationModel caller, L10Recurrence recurrence);

    Task DeleteMeeting(ISession s, L10Meeting meeting);

    Task CreatePage(ISession s, UserOrganizationModel caller, L10Recurrence.L10Recurrence_Page page);
    Task UpdatePage(ISession s, UserOrganizationModel caller, L10Recurrence.L10Recurrence_Page page);
    Task RemovePage(ISession s, UserOrganizationModel caller, L10Recurrence.L10Recurrence_Page page);

  }
}
