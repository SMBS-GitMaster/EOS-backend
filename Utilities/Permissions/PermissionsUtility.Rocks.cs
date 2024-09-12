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
    public PermissionsUtility ViewRock(long rockId, bool includeDeletedMeetings = false) {
      if (IsRadialAdmin(caller)) {
        return this;
      }
      var rock = session.Get<RockModel>(rockId);
      var userId = rock.AccountableUser.Id;
      var user = session.Get<UserOrganizationModel>(userId);

      if (user.Organization.Settings.OnlySeeRocksAndScorecardBelowYou) {
        if (caller.Id == userId) {
          return this;
        }
        if (IsManagingOrganization(user.Organization.Id)) {
          return this;
        }

        var recurQ = session.QueryOver<L10Recurrence.L10Recurrence_Rocks>();
        if (!includeDeletedMeetings) {
          recurQ = recurQ.Where(x => x.DeleteTime == null);
        }

        var recur = recurQ.Where(x => x.ForRock.Id == rockId)
                  .Select(x => x.L10Recurrence.Id)
                  .List<long>().ToList();

        if (recur.Any(x => CanViewPermitted(PermItem.ResourceType.L10Recurrence, x))) {
          return this;
        }

        return ManagesUserOrganizationOrSelf(userId);
      } else {
        return ViewUserOrganization(rock.ForUserId, false);
      }
    }

    public bool TryViewRock(long rockId, bool includeDeletedMeetings = false)
    {
      try
      {
        ViewRock(rockId, includeDeletedMeetings);
        return true;
      }
      catch (PermissionsException)
      {
        return false;
      }
    }

    private PermissionsUtility CheckEditRock(long rockId, bool alive, bool statusOnly) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      ViewRock(rockId, !alive);
      var rock = session.Get<RockModel>(rockId);
      if ((statusOnly || caller.Organization.Settings.EmployeesCanEditSelf) && rock.ForUserId == caller.Id) {
        return this;
      }
      if (caller.IsManager() && (statusOnly || caller.Organization.Settings.ManagersCanEditSelf) && rock.ForUserId == caller.Id) {
        return this;
      }

      if (caller.IsManagingOrganization() && rock.ForUserId == caller.Id && rock.OrganizationId == caller.Organization.Id) {
        return this;
      }


      //Cant edit self and not a manager.
      if (!caller.Organization.Settings.EmployeesCanEditSelf && !caller.IsManager() && rock.ForUserId == caller.Id) {
        throw new PermissionsException("Based on your organization's settings, employees cannot edit their own accountabilities. Please contact your admin.");
      }
      //Cannot edit self (manager) and is managers
      if (!caller.Organization.Settings.ManagersCanEditSelf && caller.IsManager() && rock.ForUserId == caller.Id) {
        throw new PermissionsException("Based on your organization's settings, managers cannot edit their own accountabilities. Please contact your admin.");
      }

      var recurrenceIdsQ = session.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
        .Where(x => x.ForRock.Id == rock.Id);

      if (alive) {
        recurrenceIdsQ = recurrenceIdsQ.Where(x => x.DeleteTime == null);
      } else {
        recurrenceIdsQ = recurrenceIdsQ.Where(x => x.DeleteTime != null);
      }

      var recurrenceIds = recurrenceIdsQ.Select(x => x.L10Recurrence.Id).List<long>();
      foreach (var recur in recurrenceIds) {
        try {
          EditL10Recurrence(recur);
          return this;
        } catch (PermissionsException) {
        }
      }

      if (rock.OrganizationId != caller.Organization.Id) {
        throw new PermissionsException() { NoErrorReport = true };
      }

      return ManagesUserOrganization(rock.ForUserId, true);
    }

    public PermissionsUtility EditRock(long rockId, bool statusOnly) {
      return CheckCacheFirst("EditRock", rockId, statusOnly.ToLong()).Execute(() => {
        return CheckEditRock(rockId, true, statusOnly);
      });
    }

    public PermissionsUtility EditRock_UnArchive(long rockId) {
      return CheckCacheFirst("EditRock", rockId).Execute(() => {
        return CheckEditRock(rockId, false, false);
      });
    }

    public PermissionsUtility EditMilestone(long milestoneId) {
      var rockId = session.Get<Milestone>(milestoneId).RockId;
      return EditRock(rockId, false);
    }


    public PermissionsUtility CanViewUserRocks(long userId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var user = session.Get<UserOrganizationModel>(userId);

      if (caller.Id == userId) {
        return this;
      }

      if (IsManagingOrganization(user.Organization.Id)) {
        return this;
      }

      if (IsManager(user.Organization.Id) && !user.Organization.Settings.OnlySeeRocksAndScorecardBelowYou) {
        return this;
      }

      if (user.Organization.Settings.OnlySeeRocksAndScorecardBelowYou) {
        return ManagesUserOrganizationOrSelf(userId);
      }

      return CanAdminMeetingWithUser(userId, onError: new PermissionsException().Message);


    }

    public PermissionsUtility CreateRocksForUser(long userId, long? recurrenceId = null) {
      /*
			 * ALSO UPDATE : UserPermissions.CreateRocksForUsers
			 */


      return CanAdminMeetingWithUser(userId, recurrenceId, "Cannot create goal");
    }


  }
}
