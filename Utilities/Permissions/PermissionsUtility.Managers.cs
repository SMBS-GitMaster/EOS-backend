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
		private bool IsManagingOrganization(long orgId_DoNotUse_callerOrganizationId, bool allowManagers = false) {
			if (caller.Organization.Id == orgId_DoNotUse_callerOrganizationId) {
				return caller.ManagingOrganization || (allowManagers && caller.ManagerAtOrganization && caller.Organization.ManagersCanEdit);
			}

			return false;
		}

		private bool IsManager(long organizationId) {
			if (caller.Organization.Id == organizationId) {
				return caller.ManagingOrganization || caller.ManagerAtOrganization;
			}

			return false;
    }
    public PermissionsUtility ManagesUserOrganizationOrSelf(long userOrganizationId) {
      if (userOrganizationId == caller.Id) {
        return this;
      }

      return ManagesUserOrganization(userOrganizationId, false);
    }

    public PermissionsUtility ManagerAtOrganization(long userOrganizationId, long organizationId) {
      var user = session.Get<UserOrganizationModel>(userOrganizationId);

      if (caller.Organization.Id != organizationId) {
        throw new PermissionsException();
      }

      if (user.Organization.Id == organizationId && (user.ManagerAtOrganization || user.ManagingOrganization)) {
        return this;
      }

      throw new PermissionsException();
    }
    public PermissionsUtility ManagingOrganization(long organizationId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var org = session.Get<OrganizationModel>(organizationId);

      if (IsManagingOrganization(organizationId, org.ManagersCanEdit)) {
        return this;
      }

      throw new PermissionsException();
    }
    public PermissionsUtility OwnedBelowOrEqual(long userId) {
      if (IsOwnedBelowOrEqual(caller, userId)) {
        return this;
      }

      throw new PermissionsException();
    }

    protected bool IsOwnedBelowOrEqual(UserOrganizationModel caller, long userId) {
      if (userId == caller.Id) {
        return true;
      }

      return DeepAccessor.Users.ManagesUser(session, this, caller.Id, userId);
    }


    protected bool IsOwnedBelowOrEqualOrganizational<T>(T start, Origin origin) where T : IOrigin {
      if (origin.AreEqual(start)) {
        return true;
      }

      foreach (var sub in start.OwnsOrigins()) {
        if (IsOwnedBelowOrEqualOrganizational(sub, origin)) {
          return true;
        }
      }

      return false;
    }
  }
}
