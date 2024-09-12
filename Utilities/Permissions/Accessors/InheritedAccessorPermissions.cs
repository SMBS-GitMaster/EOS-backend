using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.Accessors;
using RadialReview.Core.Models.Terms;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using static RadialReview.Utilities.PermissionsUtility;

namespace RadialReview.Utilities.Permissions.Accessors {
  public class InheritedAccessorPermissions : IAccessorPermissions {
    public IEnumerable<PermRowVM> ConstructRowViewModel(ISession s, PermissionsUtility perms, TermsCollection terms, IEnumerable<PermItem> permItems, PermRowSettings settings) {


      return permItems.Select(pi => {

        //Do not use pi.ResourceId
        var resourceType = pi.InheritType.Value;
        var resourceId = pi.AccessorId;//this is correct.

        var resourcePermissions = PermissionsUtility.GetResourcePermissionsForType(resourceType);
        var meta = resourcePermissions.GetMetaData(s, resourceId);


        var row = new PermRowVM(pi, settings) {
          Title = "Inherited permissions from " + (meta.Name ?? ("" + resourceType)) + " " + resourceType.ToFriendlyName(terms),
          Picture = meta.Picture
        };

        IEnumerable<PermRowVM> children = new List<PermRowVM>();
        try {
          children = PermissionsAccessor.GetPermRows(s, perms, resourceType, resourceId, settings.WithAllDisabled()).GetUnresolvedRows();
        } catch (PermissionsException) {
        }
        row.AddChildren(children);
        return row;
      });
    }

    public PermItem.AccessType ForAccessorType() {
      return PermItem.AccessType.Inherited;
    }

    public IEnumerable<IAccessorPermissionTest> PermissionTests() {
      yield return new InheritedPermissionTest();
    }

    public class InheritedPermissionTest : IAccessorPermissionTest {
      public bool IsPermitted(ISession session, PermissionsUtility perms, IAccessorPermissionContext data) {
        var requestedLevel = data.TestFlag;
        if (data.PermItem.AccessorType != PermItem.AccessType.Inherited) {
          throw new Exception("Fatal error. HasInheritedPermissions called incorrectly.");
        }
        requestedLevel.EnsureSingleAndValidAccessLevel();//Ill just check this twice to be super confident.
        if (data.PermItem.InheritType == null) {
          //Must be defined.
          return false;
        }

        var inheritedResourceId = data.PermItem.AccessorId;
        var inheritedType = data.PermItem.InheritType;
        var circRef = new CircularReferenceInheritedPermissions(requestedLevel, inheritedType.Value, inheritedResourceId, data.IncludeAlternateUsers);
        if (data.PermissionDataCache.ReferencesAlreadyChecked.ContainsKey(circRef)) {
          //Either already checked (returns already calculated value), or circular reference found (returns false)
          return data.PermissionDataCache.ReferencesAlreadyChecked[circRef];
        }
        //Set to false before diving so we do not encounter circular references.
        data.PermissionDataCache.ReferencesAlreadyChecked[circRef] = false;

        //We require BOTH permissions to the "inherit" permItem AND the inheritED permItem.
        if (!data.PermItem.CanView)
          requestedLevel &= ~PermItem.AccessLevel.View;
        if (!data.PermItem.CanEdit)
          requestedLevel &= ~PermItem.AccessLevel.Edit;
        if (!data.PermItem.CanAdmin)
          requestedLevel &= ~PermItem.AccessLevel.Admin;

        //Have we filterd it out?
        if (requestedLevel == PermItem.AccessLevel.Invalid) {
          return false;
        }

        PermissionsUtility _ = null;//throw away.
        var and = true;// we're only passing in one flag so this is irrelevant.



        data.PermissionDataCache.ReferencesAlreadyChecked[circRef] = perms.CanAccessItem(requestedLevel, inheritedType.Value, inheritedResourceId, null, ref _, and, data.PermissionDataCache, data.IncludeAlternateUsers);
        return data.PermissionDataCache.ReferencesAlreadyChecked[circRef];
      }

      public int OrderOfOperation() {
        return 4;
      }
    }
  }
}
