using log4net;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.EventAnalyzers.Models;
using RadialReview.Crosscutting.Zapier;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Admin;
using RadialReview.Models.Askables;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Documents;
using RadialReview.Models.Enums;
using RadialReview.Models.Integrations;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Permissions;
using RadialReview.Models.Prereview;
using RadialReview.Models.Process;
using RadialReview.Models.Rocks;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Survey;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Models.UserTemplate;
using RadialReview.Models.VTO;
using RadialReview.Reflection;
using RadialReview.Utilities.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace RadialReview.Utilities {
  public partial class PermissionsUtility {

    /// <summary>
    /// RecurrenceId is there for speedup.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="optionalRecurrenceId"></param>
    /// <param name="onError"></param>
    /// <returns></returns>
    private PermissionsUtility CanAdminMeetingWithUser(long userId, long? optionalRecurrenceId = null, string onError = "Cannot create. User is not an attendee of the meeting.") {
      if (userId == 0) {
        throw new PermissionsException(onError);
      }

      if (IsRadialAdmin(caller)) {
        return this;
      }



      var owner = session.Get<UserOrganizationModel>(userId);
      if (owner.Organization.Settings.EmployeesCanEditSelf && IsSelf(userId)) {
        return this;
      }

      try {
        return EditUserDetails(userId);
      } catch (Exception) {
      }


      if (optionalRecurrenceId == null) {
        try {
          return canAdminAnyMeetingsWithUser(userId);
        } catch (Exception) {
        }
      } else {
        CanAdmin(PermItem.ResourceType.L10Recurrence, optionalRecurrenceId.Value, includeAlternateUsers: true);
        var inMeeting = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
          .Where(x => x.DeleteTime == null && x.User.Id == userId && x.L10Recurrence.Id == optionalRecurrenceId.Value)
          .RowCount();
        if (inMeeting > 0) {
          return this;
        }
      }

      throw new PermissionsException(onError, true);
    }


    private PermissionsUtility canAdminAnyMeetingsWithUser(long userId) {
      return TryWithAlternateUsers(p => {
        //Get caller's Editable meetings
        var callerAdminRecurrenceIds = p.GetAdminMeetingForUser(caller.Id);
        //Get Attendees in that meeting.
        var inAnyAdminMeetings = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
          .Where(x => x.DeleteTime == null && x.User.Id == userId)
          .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(callerAdminRecurrenceIds.ToList())
          .RowCount();
        //is user id an attendee?
        if (inAnyAdminMeetings > 0) {
          return this;
        }
        throw new PermissionsException();
      });
    }

    public PermissionsUtility CreateL10Recurrence(long organizationId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var organization = session.Get<OrganizationModel>(organizationId);
      if (IsManagingOrganization(organizationId)) {
        return this;
      }

      if (organization.Settings.EmployeeCanCreateL10 && caller.Organization.Id == organizationId) {
        return this;
      }

      if (organization.Settings.ManagersCanCreateL10 && IsManager(organizationId)) {
        return this;
      }

      throw new PermissionsException("Cannot create meeting.");
    }

    public PermissionsUtility AdminL10Recurrence(long recurrenceId) {
      return TryWithAlternateUsers(p => {
        if (IsRadialAdmin(caller)) {
          return this;
        }

        if (recurrenceId == 0) {
          throw new PermissionsException("Meeting does not exist.");
        }

        return CanAdmin(PermItem.ResourceType.L10Recurrence, recurrenceId, exceptionMessage: "You are not an admin for this Weekly Meeting");
      });
    }

    public PermissionsUtility EditL10Recurrence(long recurrenceId) {
      return TryWithAlternateUsers(p => {
        return CheckCacheFirst("EditL10Recurrence", recurrenceId).Execute(() => {
          if (IsRadialAdmin(caller)) {
            return this;
          }

          if (recurrenceId == 0) {
            throw new PermissionsException("Meeting does not exist.");
          } else {
            var recur = session.Get<L10Recurrence>(recurrenceId);

            return CanEdit(PermItem.ResourceType.L10Recurrence, recurrenceId, (@this) => {
              var availUserIds = new[] { caller.Id };

              if (recur.CreatedById == caller.Id) {
                return this;
              }

              if (IsManagingOrganization(recur.OrganizationId)) {
                return this;
              }

              if (caller.Organization.Settings.ManagersCanEditSubordinateL10) {
                availUserIds = DeepAccessor.Users.GetSubordinatesAndSelf(session, caller, caller.Id).ToArray();
              }

              var exists = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
                .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
                .WhereRestrictionOn(x => x.User.Id).IsIn(availUserIds)
                .RowCount();
              if (exists > 0) {
                return @this;
              }

              throw new PermissionsException();
            }, "Cannot edit this Weekly Meeting");
          }
        });
      });
    }

    public PermissionsUtility ViewL10Page(long pageId) {
      var p = session.Get<L10Recurrence.L10Recurrence_Page>(pageId);
      return ViewL10Recurrence(p.L10Recurrence.Id);
    }


    public PermissionsUtility ViewL10Page(string page) {
      long pageId;
      if (long.TryParse(page.SubstringAfter("-"), out pageId)) {
        return ViewL10Page(pageId);
      }
      throw new PermissionsException();
    }

    public bool IsL10RecurrenceViewable(long recurrenceId)
    {
      try
      {
        ViewL10Recurrence(recurrenceId);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public PermissionsUtility ViewL10Recurrence(long recurrenceId) {
      return TryWithAlternateUsers(p => {
        if (IsRadialAdmin(caller)) {
          return this;
        }



        return CanView(PermItem.ResourceType.L10Recurrence, recurrenceId, (@this) => {
          var possibleUsers = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
        .Select(x => x.User.Id).List<long>()
        .ToList();

          if (possibleUsers.Contains(caller.Id)) {
            return @this;
          }

          if (caller.Organization.Settings.ManagersCanViewSubordinateL10) {
            var subIds = DeepAccessor.Users.GetSubordinatesAndSelf(session, caller, caller.Id);
            if (possibleUsers.ContainsAny(subIds)) {
              return @this;
            }
          }
          throw new PermissionsException();
        }, "Cannot view this Weekly Meeting");
      });
    }

    public PermissionsUtility CanViewL10Recurrence(TinyRecurrence recurrence) {
      return TryWithAlternateUsers(p => {
        if (IsRadialAdmin(caller)) {
          return this;
        }

        return CanView(PermItem.ResourceType.L10Recurrence, recurrence.Id, (@this) => {
          var possibleUsers = recurrence.L10Recurrence_Attendees.Select(x => x.User.Id).ToList();
          if (possibleUsers.Contains(caller.Id)) {
            return @this;
          }

          if (caller.Organization.Settings.ManagersCanViewSubordinateL10) {
            var subIds = DeepAccessor.Users.GetSubordinatesAndSelf(session, caller, caller.Id);
            if (possibleUsers.ContainsAny(subIds)) {
              return @this;
            }
          }
          throw new PermissionsException();
        }, "Cannot view this Weekly Meeting");
      });
    }

    public List<TinyRecurrence> FilterTinyRecurrencesWithCanViewPermission(List<TinyRecurrence> allRecurrences) {
      return allRecurrences.Where(recurrence => {
        try {
          CanViewL10Recurrence(recurrence);
          return true;
        } catch {
          return false;
        }
      }).ToList();

    }

    private IEnumerable<long> GetEditableMeetingForUser(long userId) {
      return GetAllPermItemsForUser(PermItem.ResourceType.L10Recurrence, userId).Where(x => x.CanEdit).Select(x => x.ResId);
    }

    private IEnumerable<long> GetAdminMeetingForUser(long userId) {
      return GetAllPermItemsForUser(PermItem.ResourceType.L10Recurrence, userId).Where(x => x.CanAdmin).Select(x => x.ResId);
    }

    [Obsolete("Avoid using")]
    public PermissionsUtility ViewUsersL10Meetings(long userId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      if (caller.Id == userId) {
        return this;
      }

      var users_OrgId = session.Get<UserOrganizationModel>(userId).Organization.Id;
      if (IsManagingOrganization(users_OrgId)) {
        return this;
      }

      if (caller.Organization.Settings.ManagersCanViewSubordinateL10) {
        var subIds = DeepAccessor.Users.GetSubordinatesAndSelf(session, caller, caller.Id);
        if (subIds.Contains(userId)) {
          return this;
        }
      }

      throw new PermissionsException();
    }

    public PermissionsUtility ViewL10Meeting(long meetingId) {
      return CheckCacheFirst("ViewL10Meeting", meetingId).Execute(() => {
        if (IsRadialAdmin(caller)) {
          return this;
        }

        var meeting = session.Get<L10Meeting>(meetingId);
        var meeting_OrgId = meeting.OrganizationId;

        return CanView(PermItem.ResourceType.L10Recurrence, meeting.L10RecurrenceId, (@this) => {
          var meetingIds = session.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x =>
            x.L10Meeting.Id == meetingId &&
            x.DeleteTime == null).List().Select(x => x.UserId).ToList();
          if (caller.UserIds.ContainsAny(meetingIds)) {
            return @this;
          }

          if (caller.Organization.Settings.ManagersCanViewSubordinateL10) {
            var subIds = DeepAccessor.Users.GetSubordinatesAndSelf(session, caller, caller.Id);
            if (subIds.ContainsAny(meetingIds)) {
              return @this;
            }
          }

          var recurId = meeting.L10RecurrenceId;
          var defaultIds = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x =>
            x.L10Recurrence.Id == recurId &&
            x.DeleteTime == null).List().Select(x => x.User.Id).ToList();

          if (caller.UserIds.ContainsAny(defaultIds)) {
            return @this;
          }

          if (caller.Organization.Settings.ManagersCanViewSubordinateL10) {
            var subIds = DeepAccessor.Users.GetSubordinatesAndSelf(session, caller, caller.Id);
            if (subIds.ContainsAny(defaultIds)) {
              return @this;
            }
          }

          throw new PermissionsException();
        }, includeAlternateUsers: true);
      });
    }

    public PermissionsUtility CanAdminMeetingItemsForUser(long userId, long recurrenceId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var user = session.Get<UserOrganizationModel>(userId);
      ViewUserOrganization(userId, false);
      var obj = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.User.Id == userId).Take(1).SingleOrDefault();

      if (obj == null) {
        throw new PermissionsException("User is not attendee.");
      }
      var canEditSelf = user.Organization.Settings.EmployeesCanEditSelf || (user.IsManager() && user.Organization.Settings.ManagersCanEditSelf);
      return Or(x => x.CanAdmin(PermItem.ResourceType.L10Recurrence, recurrenceId), x => x.ManagesUserOrganization(userId, canEditSelf));


      throw new PermissionsException("You do not manage this user.") { NoErrorReport = true };
    }

    public PermissionsUtility ViewVideoL10Recurrence(long recurrenceId) {
      return ViewL10Recurrence(recurrenceId);
    }
  }
}
