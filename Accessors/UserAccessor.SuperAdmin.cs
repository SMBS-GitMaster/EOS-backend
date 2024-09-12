using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using System;
using System.Linq;
using RadialReview.Models.Admin;

namespace RadialReview.Accessors {
  public partial class UserAccessor : BaseAccessor {
    public partial class Unsafe {
      public class CanChangeRole {
        public bool Allowed { get; set; }
        public string Message { get; set; }
        public CanChangeRole(bool allowed, string message) {
          Allowed = allowed;
          Message = message;
        }
      }

      public static UserModel GetSetAsUser(UserModel caller, UserOrganizationModel callerUserOrg, string requestedEmail, AdminAccessViewModel audit = null) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var requestedUser = UserAccessor.Unsafe.GetUserByEmail(requestedEmail);
            var isAdmin = caller != null && ((caller.IsRadialAdmin) || (callerUserOrg != null && callerUserOrg.IsRadialAdmin));
            var recordAudit = new Action(() => s.Save(audit.ToDatabaseModel(caller.Id)));
            if (!CanSetAs(isAdmin, caller.Id, requestedUser.Id, requestedEmail, new[] { "tractiontools.com", "winterinternational.io", "bloomgrowth.com" }, audit, recordAudit)) {
              throw new PermissionsException("Cannot set as other Bloom Growth users.");
            }
            s.Update(caller);
            tx.Commit();
            s.Flush();
            return requestedUser;
          }
        }
      }

      public static bool CanSetAs(bool isAdmin, string callerUserId, string requestedUserId, string requestedEmail, string[] tractionToolsEmailSubstrings, AdminAccessViewModel audit, Action onAdminAllow) {
        if (!isAdmin) {
          return false;
        } else {
          if (callerUserId == requestedUserId) {
            return true;
          }
          if (tractionToolsEmailSubstrings.Any(emailSubstring => requestedEmail.ToLower().Contains(emailSubstring.ToLower()))) {
            return false;
          }
          if (audit == null) {
            throw new AdminSetRoleException(requestedEmail);
          }
          audit.EnsureValid();
          onAdminAllow?.Invoke();
          return true;
        }
      }


      public static CanChangeRole CanChangeToRole(long requestedUserOrgId, long[] myUserOrganizations, long requestedOrganizationId, AccountType requestedOrganizationType, long[] disallowedOrgIds, bool isAdmin, AdminAccessViewModel audit, Action onAdminAllow) {
        if (!isAdmin && myUserOrganizations.Any(x => x == requestedUserOrgId)) {
          //Not an admin and has Access
          return new CanChangeRole(true, "success");
        } else if (isAdmin) {
          if (requestedOrganizationType == AccountType.SwanServices) {
            //Auto set if Swan services
            audit.EnsureValid();
            onAdminAllow?.Invoke();
            return new CanChangeRole(true, "success");
          } else if (disallowedOrgIds.Contains(requestedOrganizationId)) {
            //Only allow TT if owned..
            if (myUserOrganizations.Any(x => x == requestedUserOrgId)) {
              return new CanChangeRole(true, "success");
            }

            return new CanChangeRole(false, "Admins cannot access disallowed organizations.");
          } else {
            //Is Admin
            if (audit == null) {
              throw new AdminSetRoleException(requestedUserOrgId);
            }

            audit.EnsureValid();
            onAdminAllow?.Invoke();
            return new CanChangeRole(true, "success");
          }
        } else {
          return new CanChangeRole(false, "Cannot access.");
        }
      }

    }
  }
}
