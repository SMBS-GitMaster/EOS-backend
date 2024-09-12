using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.CrossCutting {
  public class CoachPermissions : IDeleteUserOrganizationHook, IMeetingEvents {
    public bool AbsorbErrors() {
      return true;
    }


    public bool CanRunRemotely() {
      return false;
    }
    public HookPriority GetHookPriority() {
      return HookPriority.Low;
    }

    public async Task CreateRecurrence(ISession s, L10Recurrence recur) {
      var coachOrgs = s.QueryOver<CoachOrg>()
          .Where(x => x.OrgId == recur.OrganizationId && x.DeleteTime == null)
          .List().ToList();
      foreach (var c in coachOrgs) {
        if (c.ViewEverything || c.AdminEverything) {
          PermissionsAccessor.InitializePermItems_Unsafe(
            s, s.Load<UserOrganizationModel>(recur.CreatedById),
            PermItem.ResourceType.L10Recurrence, recur.Id,
            PermTiny.RGM(c.CoachUserId, c.ViewEverything || c.AdminEverything, c.AdminEverything, c.AdminEverything)
          );
        }
      }
    }
    public async Task DeleteUser(ISession s, UserOrganizationModel user, DateTime deleteTime) {
      if (user.User != null) {
        var coachOrgs = s.QueryOver<CoachOrg>()
          .Where(x => x.CoachId == user.User.Id && x.DeleteTime == null && x.OrgId == user.Organization.Id)
          .List().ToList();
        foreach (var f in coachOrgs) {
          f.DeleteTime = deleteTime;
          s.Update(f);
        }
      }
    }

    public async Task UndeleteUser(ISession s, UserOrganizationModel user, DateTime deleteTime) {
      if (user.User != null) {
        var coachOrgs = s.QueryOver<CoachOrg>()
          .Where(x => x.CoachId == user.User.Id && x.DeleteTime == deleteTime && x.OrgId == user.Organization.Id)
          .List().ToList();
        foreach (var f in coachOrgs) {
          f.DeleteTime = null;//user.DeleteTime ?? DateTime.UtcNow;
          s.Update(f);
        }
      }
    }
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

    public async Task DeleteMeeting(ISession s, L10Meeting meeting) {
      //noop
    }

    public async Task DeleteRecurrence(ISession s, L10Recurrence recur) {
      //noop
    }
    public async Task RemoveAttendee(ISession s, long recurrenceId, long userId, List<L10Recurrence.L10Recurrence_Attendee> removedFromRecurrence, List<L10Meeting.L10Meeting_Attendee> removedFromMeeting) {
      //noop
    }

    public async Task StartMeeting(ISession s, L10Recurrence recur, L10Meeting meeting) {
      //noop
    }

    public async Task UndeleteRecurrence(ISession s, L10Recurrence recur) {
      //noop
    }

    public async Task CurrentPageChanged(ISession s, long recurrenceId, L10Meeting meeting, string pageName, double now_ms, double baseMins)
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
  }
}
