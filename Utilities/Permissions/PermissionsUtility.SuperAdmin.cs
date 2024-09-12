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

    public static bool IsAdmin(ISession s, UserOrganizationModel caller) {
      return IsRadialAdmin(s, caller);
    }

    [Obsolete("Avoid using")]
    public static PermissionsUtility CreateAdmin(ISession s) {
      return Create(s, UserOrganizationModel.CreateAdmin());
    }

    public PermissionsUtility RadialAdmin(bool allowSpecialOrgs = false) {
      if (IsRadialAdmin(caller, allowSpecialOrgs)) {
        return this;
      }

      throw new PermissionsException();
    }

    public bool HasRadialAdminFlags() {
      return TestIsAdmin(caller);
    }

    protected Boolean IsRadialAdmin(UserOrganizationModel caller, bool allowSpecialOrgs = false) {
      return IsRadialAdmin(session, caller, allowSpecialOrgs);
    }


    protected static Boolean IsRadialAdmin(ISession session, UserOrganizationModel caller, bool allowSpecialOrgs = false, bool allowAdminsWithoutAudit = false) {

      if (caller.Id == UserOrganizationModel.ADMIN_ID) {
        return true;
      }

      if (caller._IsTestAdmin) {
        return true;
      }

      allowSpecialOrgs = allowSpecialOrgs || Thread.GetData(Thread.GetNamedDataSlot("AllowSpecialOrgs")).NotNull(x => (bool)x);

      #region As an admin
      //We are an admin...
      if (TestIsAdmin(caller)) {
        if (caller._PermissionsOverrides != null) {
          if (caller._PermissionsOverrides.Admin.AllowAdminWithoutAudit) {
            return true;
          }

          if (caller._PermissionsOverrides.Admin.IsMocking) {
            //1795 = EOSWW, 1634 = TT
            if (!allowSpecialOrgs && caller.Organization != null && (Config.GetDisallowedOrgIds(session).Contains(caller.Organization.Id))) {
              return false;
            }
            //We're logged in as someone else...
            if (HasSuperAdminAccess(session, caller, caller._PermissionsOverrides.Admin.ActualUserId)) {
              return true;
            } else {//admin, but no audit log...
              throw new AdminSetRoleException(caller.Id);
            }
          } else {
            //Not mocking, we're just a standard user..
            return false;
          }
        }
      }
      #endregion
      return false;
    }

    [Obsolete("Only tests, does not ensure we are admin")]
    public static bool TestIsAdmin(UserOrganizationModel caller) {
      return caller != null && (
            (caller.IsRadialAdmin) ||
            (caller.User != null && caller.User.IsRadialAdmin) ||
            (caller._PermissionsOverrides != null && caller._PermissionsOverrides.Admin.IsRadialAdmin)
          );
    }

    public static bool HasSuperAdminAccess(ISession s, UserOrganizationModel caller, string adminId) {
      var callerId = caller.Id;
      if (TestIsAdmin(caller)) {
        var any = s.QueryOver<AdminAccessModel>()
          .Where(x => x.AccessId == callerId && x.AdminUserId == adminId)
          .Where(x => x.CreateTime <= DateTime.UtcNow.AddMinutes(10) && DateTime.UtcNow <= x.DeleteTime)
          .RowCount();
        return (any > 0);
      }
      return false;
    }
  }
}
