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
    public PermissionsUtility EditUserModel(string userId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      if (caller.User.Id == userId) {
        return this;
      }

      throw new PermissionsException();
    }

    public PermissionsUtility EditUserModel(long userId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var userOrg = session.Get<UserOrganizationModel>(userId);
      if (userOrg.User != null) {
        if (caller.User.Id == userOrg.User.Id) {
          return this;
        }

        if (CanEditPermitted(PermItem.ResourceType.EditDeleteUserDataForOrganization, userOrg.Organization.Id)) {
          return this;
        }
      }

      if (userOrg.TempUser != null) {
        if (IsPermitted(x => x.CanAddUserToOrganization(userOrg.Organization.Id))) {
          return this;
        }

        if (CanEditPermitted(PermItem.ResourceType.EditDeleteUserDataForOrganization, userOrg.Organization.Id)) {
          return this;
        }
      }

      throw new PermissionsException();
    }



    public PermissionsUtility EditUserOrganization(long userId) {
      if (caller.Id == userId) {
        return this;
      }

      return ManagesUserOrganization(userId, false);

    }

    public PermissionsUtility ViewUserOrganization(long userOrganizationId, Boolean sensitive) {
      return TryWithAlternateUsers(p => {
        return CheckCacheFirst("ViewUserOrganization", userOrganizationId, sensitive.ToLong()).Execute(() => {
          if (IsRadialAdmin(caller)) {
            return this;
          }

          var userOrg = session.Get<UserOrganizationModel>(userOrganizationId);
          if (userOrg == null) {
            throw new PermissionsException();// "User does not exist. (" + userOrganizationId + ")");
          }

          if (userOrg.User != null && caller.User != null && !string.IsNullOrWhiteSpace(userOrg.User.Id) && !string.IsNullOrWhiteSpace(caller.User.Id) && userOrg.User.Id == caller.User.Id) {
            //Added (same parent user)
            return this;
          }


          if (IsManagingOrganization(userOrg.Organization.Id)) {
            return this;
          }

          if (sensitive) {
            if (userOrganizationId == caller.Id) {
              return this;
            }

            return ManagesUserOrganization(userOrganizationId, false);
          } else {
            if (userOrg.Organization.Id == caller.Organization.Id) {
              return this;
            }
          }

          throw new PermissionsException();
        });
      });
    }


    [Todo]
    public PermissionsUtility ManagesForModel(IForModel forModel, bool disableIfSelf) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      if (forModel.Is<UserOrganizationModel>()) {
        return ManagesUserOrganization(forModel.ModelId, disableIfSelf);
      } else if (forModel.Is<AccountabilityNode>()) {
        if (disableIfSelf) {
          var ac = session.Get<AccountabilityNode>(forModel.ModelId);
          if (ac.GetUsers(session).Any(x => x.Id == caller.Id)) {
            //Cannot manage self.
            throw new PermissionsException("Cannot access. You do not manage this.");
          }
        }
        return ManagesAccountabilityNodeOrSelf(forModel.ModelId);
      } else if (forModel.Is<OrganizationModel>()) {
        return ManagingOrganization(forModel.ModelId);
      } else if (forModel.Is<SurveyUserNode>()) {
        var sun = session.Get<SurveyUserNode>(forModel.ModelId);
        return ManagesForModel(sun.AccountabilityNode, disableIfSelf);
      }

      throw new PermissionsException("Unrecognized ForModel type.");
    }


    public PermissionsUtility ManagesUserOrganization(long userOrganizationId, bool disableIfSelf) {
      /*
			 * ALSO UPDATE: MultiUserManagesUserOrganizations
			 */

      return TryWithOverrides(p => {
        if (IsRadialAdmin(caller)) {
          return this;
        }
        var user = session.Get<UserOrganizationModel>(userOrganizationId);

        if (user == null) {
          throw new PermissionsException();//"User does not exist.");
        }

        if (IsManagingOrganization(user.Organization.Id, true)) {
          return this;
        }

        if (caller.ManagingOrganization) {
          var subordinate = session.Get<UserOrganizationModel>(userOrganizationId);
          if (user != null && user.Organization.Id == caller.Organization.Id) {
            return this;
          }
        }

        if (disableIfSelf && caller.Id == userOrganizationId) {
          throw new PermissionsException("You cannot do this to yourself.") { NoErrorReport = true };
        }

        //..was here

        //Confirm this is correct. Do you want children to also be managers?
        if (caller.IsManager() && DeepAccessor.Users.GetSubordinatesAndSelf(session, caller, caller.Id).Any(x => x == userOrganizationId)) {
          return this;
        }

        throw new PermissionsException("You do not manage this user.") { NoErrorReport = true };

      });
    }


    public PermissionsUtility EditUserDetails(long forUserId) {
      /*
			 * ALSO UPDATE: MultiUserEditUserDetails
			 */
      return TryWithOverrides(x => {
        try {
          return ManagesUserOrganization(forUserId, true);
        } catch (PermissionsException) {
          var foundUser = session.Get<UserOrganizationModel>(forUserId);
          if (foundUser.Id == caller.Id && ((foundUser.ManagerAtOrganization && foundUser.Organization.Settings.ManagersCanEditSelf) || foundUser.Organization.Settings.EmployeesCanEditSelf || foundUser.ManagingOrganization)) {
            return this;
          }
        }
        throw new PermissionsException("Cannot edit for user.");
      });
    }

  }
}
