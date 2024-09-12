using NHibernate;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Interfaces
{
  public interface IMeetingAttendeeHooks : IHook
  {
    Task UpdateAttendee(ISession session, UserOrganizationModel caller, long recurrenceId, L10Recurrence.L10Recurrence_Attendee attendee);
  }
}
