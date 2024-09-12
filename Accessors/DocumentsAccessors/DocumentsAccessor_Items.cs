using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Documents;
using RadialReview.Models.Downloads;
using RadialReview.Utilities;
using RadialReview.Utilities.FileTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
  public partial class DocumentsAccessor {

    private static long? GetItemOrgId_Unsafe(ISession s, TinyDocumentItem item) {
      var method = GetQueryMethodsForType(item.ItemType);
      return method.GetOrgId_Unsafe(s, item.ItemId);
    }

    private static IEnumerable<DocumentItemVM> GetItems_Unsafe(ISession s, DocumentsFolder folder, List<TinyDocumentItem> items) {
      if (items == null) {
        return new List<DocumentItemVM>();
      }

      var queries = new List<IEnumerable<DocumentItemVM>>();
      foreach (var method in QueryMethods) {
        var itemType = method.ForItemType();
        var ids = items.Where(x => x.ItemType == itemType).Select(x => x.ItemId).ToList();
        if (ids.Any()) {
          //Add query to list.
          var query = method.GetDocumentItemsById_Unsafe(s, ids, new DocumentItemSettings(folder));
          queries.Add(query);
        }
      }
      return queries.SelectMany(x => x);

    }

    private static IEnumerable<DocumentItemVM> GetDocumentItemVMs_Unsafe(ISession s, DocumentsFolder folder, ItemIdCriterionGenerator generator) {

      var queries = new List<IEnumerable<DocumentItemVM>>();
      foreach (var method in QueryMethods) {
        var itemType = method.ForItemType();
        var query = method.GetDocumentItemsWhereIdInQuery_Unsafe(s, generator(itemType), new DocumentItemSettings(folder));
        queries.Add(query);
      }
      return queries.SelectMany(x => x);
    }

    public static async Task<bool> DeleteItem(UserOrganizationModel caller, long itemId, DocumentItemType itemType, string folderId) {
      return await _DeleteItem(caller, itemId, itemType, folderId, false);
    }
    private static async Task<bool> _DeleteItem(UserOrganizationModel caller, long itemId, DocumentItemType itemType, string fromFolderGuid, bool isMove) {
      var tiny = new TinyDocumentItem(itemId, itemType);
      await EnsureDeletePermitted_1(caller, tiny, isMove);

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var fromFolder = GetDocumentFolder_Unsafe(s, fromFolderGuid);
          var fromFolderId = fromFolder.Id;

          await EnsureDeletePermitted_2(isMove, s, perms, tiny, fromFolderId);

          var found = s.QueryOver<DocumentItemLocation>()
            .Where(x => x.DeleteTime == null && x.DocumentFolderId == fromFolderId && x.ItemId == tiny.ItemId && x.ItemType == tiny.ItemType )
            .Take(1).SingleOrDefault();

          //Could be generated...
          //if (found == null)
          //	throw new PermissionsException("Could not find " + itemType.ToFriendlyName());

          var now = DateTime.UtcNow;
          if (found != null) {
            await _DeleteLink_Unsafe(s, caller, fromFolder, found, now);
          }

          if (itemType == DocumentItemType.EncryptedFile) {
            var anyLeft = s.QueryOver<DocumentItemLocation>()
               .Where(x => x.DeleteTime == null && x.ItemId == itemId && x.ItemType == itemType)
               .Take(1).SingleOrDefault();
            if (anyLeft == null) {
              var file = s.Get<EncryptedFileModel>(itemId);
              file.DeleteTime = now;
              s.Update(file);
            }
          }


          tx.Commit();
          s.Flush();
          return true;
        }
      }
    }


    public static DocumentItemInfoVM GetInfo(UserOrganizationModel caller, long id, DocumentItemType type) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);

          EnsureCanAccess(s, perms, PermItem.AccessLevel.View, new TinyDocumentItem(id, type));

          var items = GetItems_Unsafe(s, null, new List<TinyDocumentItem>() { new TinyDocumentItem(id, type) });
          var first = items.FirstOrDefault();
          if (first == null)
            throw new PermissionsException("Info could not be found.");

          string fileExt = first.Extension;
          string fileType = null;
          string fileTypeDetails = null;
          if (fileExt != null) {
            var ext = FileTypeExtensionUtility.GetFileTypeFromExtension(fileExt);
            fileType = ext.Kind;
            fileTypeDetails = ext.FullName;
          }

          return new DocumentItemInfoVM() {
            Name = first.Name,
            CreateTime = caller.GetTimeSettings().ToFriendlyLocalDate(first.CreateTime, "unknown"),
            Description = first.Description,
            Type = first.Type.GetFriendlyName(),
            FileType = fileType,
            FileExtension = fileExt,
            FileTypeDetails = fileTypeDetails,
            Generated = first.Generated,
            Size = first.Size
          };
        }
      }
    }

    public static async Task<bool> Rename(UserOrganizationModel caller, long id, DocumentItemType type, string name) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          EnsureCanAccess(s, perms, PermItem.AccessLevel.Edit, new TinyDocumentItem(id, type), "Cannot rename this item");

          var res = await GetQueryMethodsForType(type).Rename(s, id, name);
          if (res == false) {
            throw new PermissionsException("Failed to edit name");
          }

          tx.Commit();
          s.Flush();
          return res;
        }
      }
    }

    public static async Task CloneItem(UserOrganizationModel caller, long itemId, DocumentItemType itemType, string toFolderGuid) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);

          var methods = GetQueryMethodsForType(itemType);
          var possibleToClone = await methods.PossibleToClone(s, itemId);
          if (!possibleToClone)
            throw new PermissionsException("Cannot clone an item of this type:"+itemType);

          var tiny = new TinyDocumentItem(itemId, itemType);
          var toFolder = GetDocumentFolder_Unsafe(s, toFolderGuid);
          var toFolderId = toFolder.Id;
          await EnsureLinkPermitted(s, perms, tiny, toFolderId, false);

          var newItemId = await methods.Clone_Unsafe(s, itemId, toFolder.OrgId, caller.Id);
          await _SaveLink_Unsafe(s, caller, toFolder, newItemId, itemType, toFolder.OrgId, false);
          var permItemType = ConvertDocumentItemTypeToPermResourceType(itemType);
          PermissionsAccessor.InitializePermItems_Unsafe(s, caller, permItemType, newItemId, PermTiny.InheritedFromDocumentFolder(toFolder.Id), PermTiny.Creator(true, true, true));

          tx.Commit();
          s.Flush();
        }
      }
    }

    public static async Task LinkItem(UserOrganizationModel caller, long itemId, DocumentItemType itemType, string toFolderGuid) {
      await _LinkItem(caller, itemId, itemType, toFolderGuid, false, true);
    }

    private static async Task _LinkItem(UserOrganizationModel caller, long itemId, DocumentItemType itemType, string toFolderGuid, bool isMove, bool isShortcut) {

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var toFolder = GetDocumentFolder_Unsafe(s, toFolderGuid);
          var toFolderId = toFolder.Id;
          var tiny = new TinyDocumentItem(itemId, itemType);
          await EnsureLinkPermitted(s, perms, tiny, toFolderId, isMove);

          var orgId = GetItemOrgId_Unsafe(s, tiny) ?? toFolder.OrgId;

          await _SaveLink_Unsafe(s, caller, toFolder, itemId, itemType, orgId, isShortcut);

          tx.Commit();
          s.Flush();
        }
      }

    }

    public static async Task<DocumentItemLocation> _SaveLink_Unsafe(ISession s, UserOrganizationModel caller, DocumentsFolder toFolder, EncryptedFileModel file, long orgId, bool isShortcut, DateTime? now = null) {
      return await _SaveLink_Unsafe(s, caller, toFolder, file.Id, DocumentItemType.EncryptedFile, orgId, isShortcut, now);
    }
    public static async Task<DocumentItemLocation> _SaveLink_Unsafe(ISession s, UserOrganizationModel caller, DocumentsFolder toFolder, WhiteboardModel whiteboard, long orgId, bool isShortcut, DateTime? now = null) {
      return await _SaveLink_Unsafe(s, caller, toFolder, whiteboard.Id, DocumentItemType.Whiteboard, orgId, isShortcut, now);
    }
    public static async Task<DocumentItemLocation> _SaveLink_Unsafe(ISession s, UserOrganizationModel caller, DocumentsFolder parentFolder, DocumentsFolder childFolder, long orgId, bool isShortcut, DateTime? now = null) {
      return await _SaveLink_Unsafe(s, caller, parentFolder, childFolder.Id, DocumentItemType.DocumentFolder, orgId, isShortcut, now);
    }

    public static async Task<DocumentItemLocation> _SaveLink_Unsafe(ISession s, UserOrganizationModel caller, DocumentsFolder toFolder, long itemId, DocumentItemType itemType, long orgId, bool isShortcut, DateTime? now = null) {
      var res = new DocumentItemLocation() {
        CreateTime = now ?? DateTime.UtcNow,
        DocumentFolderId = toFolder.Id,
        ItemId = itemId,
        ItemType = itemType,
        IsShortcut = isShortcut,
        SourceOrganizationId = orgId
      };

      foreach (var interceptor in FolderInterceptors) {
        if (interceptor.ShouldExecute(s, toFolder)) {
          await interceptor.OnAfterLink(s, caller, toFolder, res);
        }
      }

      s.Save(res);
      return res;
    }
    public static async Task _DeleteLink_Unsafe(ISession s, UserOrganizationModel caller, DocumentsFolder fromFolder, DocumentItemLocation found, DateTime now) {
      found.DeleteTime = now;
      s.Update(found);

      foreach (var interceptor in FolderInterceptors) {
        if (interceptor.ShouldExecute(s, fromFolder)) {
          await interceptor.OnAfterUnlink(s, caller, fromFolder, found);
        }
      }
    }


    public static async Task MoveItem(UserOrganizationModel caller, long itemId, DocumentItemType itemType, string fromFolderGuid, string toFolderGuid) {
      //This is a composite action. We should check both sets of 
      //permissions to make sure we don't perform half an operation.
      //We could make more robust by having it revert on failure.

      //First check both link and delete permissions.
      var tiny = new TinyDocumentItem(itemId, itemType);
      //Delete precheck.
      await EnsureDeletePermitted_1(caller, tiny, true);

      bool isShortcut = false;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var fromFolderId = GetDocumentFolderId_Unsafe(s, fromFolderGuid);
          var toFolderId = GetDocumentFolderId_Unsafe(s, toFolderGuid);
          if (fromFolderId == toFolderId) {
            throw new PermissionsException("Cannot move item to the same folder.");
          }

          //Delete and link permissions
          await EnsureDeletePermitted_2(true, s, perms, tiny, fromFolderId);
          await EnsureLinkPermitted(s, perms, tiny, toFolderId, true);

          //Get some shortcut data while db is open.
          var existingLink = s.QueryOver<DocumentItemLocation>()
            .Where(x => x.DeleteTime == null && x.DocumentFolderId == fromFolderId && x.ItemId == itemId && x.ItemType == itemType)
            .Take(1).SingleOrDefault();
          isShortcut = existingLink.IsShortcut;
        }
      }

      //Then perform both actions.
      await _LinkItem(caller, itemId, itemType, toFolderGuid, true, isShortcut);
      await _DeleteItem(caller, itemId, itemType, fromFolderGuid, true);


    }


    private static async Task EnsureDeletePermitted_1(UserOrganizationModel caller, TinyDocumentItem tiny, bool isMove) {
      if (tiny.ItemType == DocumentItemType.DocumentFolder) {
        await EnsureCanDeleteFolder(caller, tiny.ItemId, isMove);
      }
    }

    private static async Task EnsureDeletePermitted_2(bool isMove, ISession s, PermissionsUtility perms, TinyDocumentItem tiny, long fromFolderId) {
      var operation = "delete";
      var operating = "deleting";
      var folderErrName = "folder";
      if (isMove) {
        operation = "move";
        operating = "moving";
        folderErrName = "source folder";
      }
      EnsureCanAccess(s, perms, PermItem.AccessLevel.Admin, tiny, $@"{operating.ToTitleCase()} requires Admin permissions over the selected {tiny.ItemType.GetFriendlyName()}, cannot  {operation}.");
      perms.EditDocumentsFolder(fromFolderId, $@"This operation requires Edit permissions over the {folderErrName}.");
    }

    private static async Task EnsureLinkPermitted(ISession s, PermissionsUtility perms, TinyDocumentItem tiny, long toFolderId, bool isMove) {
      var operate = "copy";
      var operating = "copying";
      if (isMove) {
        operate = "move";
        operating = "moving";
      }
      EnsureCanAccess(s, perms, PermItem.AccessLevel.View, tiny, $@"Cannot {operate} this {tiny.ItemType.GetFriendlyName()}. {operating.ToTitleCase()} requires View permissions.");
      EnsureCanAccess(s, perms, PermItem.AccessLevel.Edit, new TinyDocumentItem(toFolderId, DocumentItemType.DocumentFolder), "Cannot edit destination folder.");
      var existing = s.QueryOver<DocumentItemLocation>().Where(x => x.DeleteTime == null && x.DocumentFolderId == toFolderId && x.ItemId == tiny.ItemId && x.ItemType == tiny.ItemType).RowCount() != 0;
      if (existing) {
        throw new PermissionsException("Item already in destination.");
      }
    }
  }
}