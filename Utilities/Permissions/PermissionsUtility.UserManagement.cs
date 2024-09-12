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
    public PermissionsUtility RemoveUser(long userId) {

      return TryWithOverrides(y => {
        var found = session.Get<UserOrganizationModel>(userId);
        return CanEdit(PermItem.ResourceType.EditDeleteUserDataForOrganization, found.Organization.Id);
        throw new PermissionsException("You cannot remove this user.");
      });
    }

    public PermissionsUtility CanRemoveUsersFromOrganization(long organizationId) {
      return TryWithOverrides(y => {
        return CanEdit(PermItem.ResourceType.EditDeleteUserDataForOrganization, organizationId);
        throw new PermissionsException("You cannot remove users from this organization.");
      });
    }

    public PermissionsUtility CanAddUserToOrganization(long orgId) {
      if (!IsRadialAdmin(caller) && caller.User.EmailNotVerified) {
        throw new PermissionsException("Your email must be verified before adding users.<br/><a style='text-shadow: 0px 0px 1px #0000ff;text-decoration: underline;' href='javascript: void(0)' onclick='VerifyEmail()'>Resend Verification Link</a>") {
          DurationMS = 60000,
        };
      }
      try {
        return CanEdit(PermItem.ResourceType.UpgradeUsersForOrganization, orgId);
      } catch (Exception) {
        throw new PermissionsException("You are not permitted to create users.");
      }
    }
    public PermissionsUtility CanUpgradeUser(long userId) {
      ViewUserOrganization(userId, false);
      var user = session.Get<UserOrganizationModel>(userId);
      return CanEdit(PermItem.ResourceType.UpgradeUsersForOrganization, user.Organization.Id, exceptionMessage: "This user is '" + Config.ReviewName().ToLower() + " only' and cannot be added to a Weekly Meeting. You are not permitted to upgrade them.");
    }

    public PermissionsUtility CanUpgradeUsersAtOrganization(long orgId) {
      return CanEdit(PermItem.ResourceType.UpgradeUsersForOrganization, orgId, exceptionMessage: "You're not permitted to increase the user count.");
    }

  }
}
