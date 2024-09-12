using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Documents;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
  public partial class DocumentsAccessor {


    private static List<TinyDocumentItem> WherePermitted(ISession s, PermissionsUtility perms, PermItem.AccessLevel accessLevel, List<TinyDocumentItem> items) {
      if (perms.IsPermitted(x => x.RadialAdmin())) {
        return items.ToList();
      }

      accessLevel.EnsureSingleAndValidAccessLevel();
      var results = new List<TinyDocumentItem>();
      var checkedTypes = new HashSet<DocumentItemType>();
      foreach (var method in QueryMethods) {
        var itemType = method.ForItemType();
        checkedTypes.Add(itemType);
        //do not check forced items.
        var toQuery = items.Where(x => x.ItemType == itemType && !x.ForcePermitted(accessLevel)).Select(x => x.ItemId).ToArray();
        Dictionary<long, bool> allowed = new Dictionary<long, bool>();
        if (toQuery.Any()) {
          switch (accessLevel) {
            case PermItem.AccessLevel.View:
              allowed = method.BulkView(s, perms, toQuery);
              break;
            case PermItem.AccessLevel.Edit:
              allowed = method.BulkEdit(s, perms, toQuery);
              break;
            case PermItem.AccessLevel.Admin:
              allowed = method.BulkAdmin(s, perms, toQuery);
              break;
            default:
              throw new ArgumentOutOfRangeException(nameof(accessLevel));
          }
        }
        var matchingItems = items.Where(x => x.ItemType == itemType && allowed != null && allowed.ContainsKey(x.ItemId) && allowed[x.ItemId]);
        results.AddRange(matchingItems.ToList());
      }
      if (items.Count != results.Count) {
        var itemTypes = items.Where(x => !x.ForcePermitted(accessLevel)).Select(x => x.ItemType).ToList();
        var missing = itemTypes.Where(it => !checkedTypes.Contains(it)).Distinct().ToList();
        if (missing.Any() && Config.IsLocal()) {
          throw new PermissionsException("Failed to check types:" + string.Join(",", missing) + " (" + accessLevel + ")");
        }
      }

      //Add forced items
      results.AddRange(items.Where(x => x.ForcePermitted(accessLevel)).ToList());



      return results;
    }

    private static List<DocumentItemVM> WherePermitted(ISession s, PermissionsUtility perms, PermItem.AccessLevel accessLevel, List<DocumentItemVM> items) {
      var tiny = items.Select(x => x.TinyItem).ToList();
      var allowed = WherePermitted(s, perms, accessLevel, tiny);

      var res = items.Where(i => allowed.Any(a => a.ItemId == i.TinyItem.ItemId && a.ItemType == i.TinyItem.ItemType)).ToList();
      if (res.Count != items.Count) {
        var removed = items.Where(i => !allowed.Any(a => a.ItemId == i.TinyItem.ItemId && a.ItemType == i.TinyItem.ItemType)).ToList();
        int b = 0;
      }
      return res;
    }

    private static async Task EnsureCanDeleteFolder(UserOrganizationModel caller, long folderId, bool isMove) {
      string itemLookup;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          itemLookup = s.Get<DocumentsFolder>(folderId).LookupId;
        }
      }
      await EnsureCanDeleteFolder(caller, itemLookup, isMove);
    }


    private static async Task EnsureCanDeleteFolder(UserOrganizationModel caller, string folderId, bool isMove) {

      var operate = "delete";
      var operating = "deleteing";
      var operated = "deleted";

      if (isMove) {
        operate = "move";
        operating = "moveing";
        operated = "moved";
      }

      var folder = await GetFolderContents(caller, folderId);

      if (isMove) {
        if (folder.Folder.CanAdmin == false) {
          throw new PermissionsException($@"You are not permitted to {operate} this folder. This operation requires Admin permissions.");
        }
      } else {
        if (folder.Folder.CanAdmin == false) {
          throw new PermissionsException($@"You are not permitted to {operate} this folder. This operation requires Admin permissions.");
        }
        if (folder.Folder.CanDelete == false) {
          throw new PermissionsException($@"This folder cannot be {operated}.");
        }
        if (folder.Folder.CanDelete == null) {
          throw new PermissionsException($@"Could not determine permissions, {operate} failed.");
        }
        if (folder.Contents.Any(x => x.CanDelete == false)) {
          throw new PermissionsException($@"This folder contains an item which you are not permitted to {operate}.");
        }
      }


      if (folder.ContainsHiddenItems)
        throw new PermissionsException($@"This folder contains hidden items that cannot be {operated}.");
      if (folder.Contents.Any(x => x.Generated)) {
        throw new PermissionsException($@"This folder cannot be {operated}. It contains generated content important for Bloom Growth to function.");
      }


      try {
        foreach (var child in folder.Contents) {
          if (child.IsFolder()) {
            await EnsureCanDeleteFolder(caller, child.Id, isMove);
          }
        }
      } catch (PermissionsException e) {
        throw new PermissionsException($@"This folder contains an item which you are not permitted to {operate}.");
      }
    }

    public static void EnsureCanAccess(UserOrganizationModel caller, PermItem.AccessLevel accessLevel, TinyDocumentItem item, string errorMessage = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          EnsureCanAccess(s, perms, accessLevel, item, errorMessage);
        }
      }
    }

    public static void EnsureCanAccess(ISession s, PermissionsUtility perms, PermItem.AccessLevel accessLevel, TinyDocumentItem item, string errorMessage = null) {
      if (item != null) {
        var res = WherePermitted(s, perms, accessLevel, new List<TinyDocumentItem>() { item });
        if (res.Count() == 1 && item == res[0]) {
          return;
        }
      }
      throw new PermissionsException(errorMessage ?? ("You cannot " + accessLevel + " this " + item.ItemType.GetFriendlyName()));
    }

    /*
		private static void EnsureCanEdit(ISession s, PermissionsUtility perms, TinyDocumentItem item) {
			if (item != null) {
				var methods = QueryMethods.Where(x => x.ForItemType() == item.ItemType).ToList();
				foreach (var m in methods) {
					try {
						m.EnsureCanEdit(s, perms, item.ItemId);
						return;
					} catch (PermissionsException) {
					}
				}
			}
			throw new PermissionsException("Cannot edit this " + item.ItemType.GetFriendlyName());
		}
		private static void EnsureCanAdmin(ISession s, PermissionsUtility perms, TinyDocumentItem item) {
			if (item != null) {
				var methods = QueryMethods.Where(x => x.ForItemType() == item.ItemType).ToList();
				foreach (var m in methods) {
					try {
						m.EnsureCanAdmin(s, perms, item.ItemId);
						return;
					} catch (PermissionsException) {
					}
				}
			}
			throw new PermissionsException("Cannot admin this " + item.ItemType.GetFriendlyName());
		}

		private class Permitted {

			public static bool ToView(ISession s, PermissionsUtility perms, TinyDocumentItem item) {
				try {
					EnsureCanView(s, perms, item);
					return true;
				} catch (PermissionsException) {
					return false;
				}
			}
			public static bool ToEdit(ISession s, PermissionsUtility perms, TinyDocumentItem item) {
				try {
					EnsureCanEdit(s, perms, item);
					return true;
				} catch (PermissionsException) {
					return false;
				}
			}
			public static bool ToAdmin(ISession s, PermissionsUtility perms, TinyDocumentItem item) {
				try {
					EnsureCanAdmin(s, perms, item);
					return true;
				} catch (PermissionsException) {
					return false;
				}
			}
		}*/

  }
}