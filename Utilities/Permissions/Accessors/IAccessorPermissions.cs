using NHibernate;
using RadialReview.Core.Models.Terms;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Permissions;
using RadialReview.Utilities.Permissions.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using static RadialReview.Utilities.PermissionsUtility;

namespace RadialReview.Utilities.Permissions.Accessors {


  public interface IAccessorPermissions {

    PermItem.AccessType ForAccessorType();
    IEnumerable<PermRowVM> ConstructRowViewModel(ISession s, PermissionsUtility perms, TermsCollection terms, IEnumerable<PermItem> permItems, PermRowSettings settings);

    IEnumerable<IAccessorPermissionTest> PermissionTests();

  }
  public class IAccessorPermissionContextFactory {
    public IAccessorPermissionContextFactory(long resourceId, IResourcePermissions resourcePermissions, bool includeAlternateUsers, PermissionDataCache permissionDataCache) {
      ResourceId = resourceId;
      ResourcePermissions = resourcePermissions;
      IncludeAlternateUsers = includeAlternateUsers;
      PermissionDataCache = permissionDataCache;
    }

    public long ResourceId { get; set; }
    public IResourcePermissions ResourcePermissions { get; set; }
    public bool IncludeAlternateUsers { get; set; }
    public PermissionDataCache PermissionDataCache { get; set; }

    public IAccessorPermissionContext CreateContext(PermItem permItem, PermItem.AccessLevel testFlag) {
      return new IAccessorPermissionContext(this, permItem, testFlag);
    }
  }
  public class IAccessorPermissionContext {
    public PermItem PermItem { get; set; }
    public PermItem.AccessLevel TestFlag { get; set; }
    public IResourcePermissions ResourcePermissions { get; private set; }
    public long ResourceId { get; private set; }
    public bool IncludeAlternateUsers { get; private set; }
    public PermissionDataCache PermissionDataCache { get; private set; }

    [Obsolete("Use IAccessorPermissionContextFactory.CreateContext instead")]
    public IAccessorPermissionContext(IAccessorPermissionContextFactory baseData, PermItem permItem, PermItem.AccessLevel testFlag) {
      PermItem = permItem;
      TestFlag = testFlag;
      ResourcePermissions = baseData.ResourcePermissions;
      ResourceId = baseData.ResourceId;
      IncludeAlternateUsers = baseData.IncludeAlternateUsers;
      PermissionDataCache = baseData.PermissionDataCache;
    }
  }

  public interface IAccessorPermissionTest {
    int OrderOfOperation();
    bool IsPermitted(ISession session, PermissionsUtility perms, IAccessorPermissionContext data);
  }



  //Helper methods.
  public static class IAccessorPermissionsHelpers {

    public class RgmLookup {
      public RgmLookup(IEnumerable<ResponsibilityGroupModel> backing, PermissionsUtility perms) {
        Backing = backing;
        Perms = perms;
      }
      public IEnumerable<ResponsibilityGroupModel> Backing { get; set; }
      public PermissionsUtility Perms { get; set; }

      public T GetField<T>(long id, Func<ResponsibilityGroupModel, T> field, T deflt) where T : class {
        return _GetField(id, field, deflt, false);
      }
      public T GetFieldSkipPermissions<T>(long id, Func<ResponsibilityGroupModel, T> field, T deflt) where T : class {
        return _GetField(id, field, deflt, true);
      }
      public ResponsibilityGroupModel Get(long id) {
        return _Get(id, false);
      }
      public ResponsibilityGroupModel GetSkipPermissions(long id) {
        return _Get(id, true);
      }

      private T _GetField<T>(long id, Func<ResponsibilityGroupModel, T> field, T deflt, bool skipPermissions) where T : class {
        return _Get(id, skipPermissions).NotNull(field) ?? deflt;
      }
      private ResponsibilityGroupModel _Get(long id, bool skipPermissions) {
        var found = Backing.FirstOrDefault(x => x.Id == id);
        if (skipPermissions || Perms.IsPermitted(x => x.ViewRGM(id))) {
          return found;
        }
        return null;
      }
    }

    public static RgmLookup CreateRgmLookup(this IAccessorPermissions self, ISession s, PermissionsUtility perms, IEnumerable<PermItem> permItems) {
      var accessorIds = permItems.Select(x => x.AccessorId).Distinct().ToArray();
      IEnumerable<ResponsibilityGroupModel> rgms = new List<ResponsibilityGroupModel>();
      if (accessorIds.Any(x => x > 0)) {
        rgms = s.QueryOver<ResponsibilityGroupModel>()
             .Where(x => x.DeleteTime == null)
             .AndRestrictionOn(x => x.Id)
             .IsIn(accessorIds)
             .Future();
      }
      return new RgmLookup(rgms, perms);
    }
    public static RgmLookup CreateRgmLookup(this IAccessorPermissions self, ISession s, PermissionsUtility perms, IEnumerable<long> accessorIds) {
      IEnumerable<ResponsibilityGroupModel> rgms = new List<ResponsibilityGroupModel>();
      if (accessorIds.Any(x => x > 0)) {
        rgms = s.QueryOver<ResponsibilityGroupModel>()
             .Where(x => x.DeleteTime == null)
             .AndRestrictionOn(x => x.Id)
             .IsIn(accessorIds.Distinct().ToArray())
             .Future();
      }
      return new RgmLookup(rgms, perms);
    }
  }


}
