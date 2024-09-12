using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Permissions;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Utilities.Permissions.Accessors {
  public class RgmAccessorPermissions : IAccessorPermissions {

    public PermItem.AccessType ForAccessorType() {
      return PermItem.AccessType.RGM;
    }

		public IEnumerable<PermRowVM> ConstructRowViewModel(ISession s, PermissionsUtility perms, TermsCollection terms, IEnumerable<PermItem> permItems, PermRowSettings settings) {
			var rgms = this.CreateRgmLookup(s, perms, permItems);

      return permItems.Select(pi => {
        var res = new PermRowVM(pi, settings) {
          Title = rgms.GetField(pi.AccessorId, x => x.GetNameExtended(), "-unnamed-"),
          Picture = PictureViewModel.CreateFrom(rgms.Get(pi.AccessorId)),
        };

        return res;
      });
    }

    public IEnumerable<IAccessorPermissionTest> PermissionTests() {
      yield return new RgmUserTest();
      yield return new RgmNonUserTest();
    }


    public class RgmUserTest : IAccessorPermissionTest {
      public bool IsPermitted(ISession session, PermissionsUtility perms, IAccessorPermissionContext data) {
        var caller = perms.GetCaller();
        var accessorId = data.PermItem.AccessorId;
        if (accessorId == caller.Id)
          return true;

        if (caller.Organization!=null &&caller.Organization.Id>0 && accessorId == caller.Organization.Id)
          return true;

        if (data.IncludeAlternateUsers) {
          if (caller.UserIds.Contains(accessorId)) {
            if (data.PermissionDataCache.UserIsValid(session, accessorId)) {
              return true;
            }
          }
        }
        return false;
      }

      public int OrderOfOperation() {
        return 1;
      }
    }

    public class RgmNonUserTest : IAccessorPermissionTest {
      public bool IsPermitted(ISession session, PermissionsUtility perms, IAccessorPermissionContext data) {
        var caller = perms.GetCaller();
        if (data.PermItem.AccessorId == caller.Id)
          return false;
        //Expensive, cache results
        var groups = data.PermissionDataCache.GetResponsibilityGroupsForCaller(session, perms, data.IncludeAlternateUsers);
        return groups.Any(group => data.PermItem.AccessorId == group.RgmId);
      }

      public int OrderOfOperation() {
        return 6;
      }
    }
  }
}
