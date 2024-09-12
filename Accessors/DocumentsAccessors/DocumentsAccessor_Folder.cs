using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Documents;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadialReview.Models.Documents.Enums;
using RadialReview.Models.Documents.Interceptors.Data;
using RadialReview.Models.Permissions;

namespace RadialReview.Accessors {
  public partial class DocumentsAccessor {
    public static async Task<DocumentItemVM> CreateFolder(UserOrganizationModel caller, string name, string parentFolderId, string description = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.CreateFileUnderDocumentsFolder(parentFolderId);
          var now = DateTime.UtcNow;
          var parent = GetDocumentFolder_Unsafe(s, parentFolderId);
          var f = new DocumentsFolder() {
            CanDelete = true,
            CreateTime = now,
            CreatorId = caller.Id,
            Description = description,
            Generated = false,
            Name = name,
            OrgId = parent.OrgId,
            Root = false,
          };

          s.Save(f);


          await DocumentsAccessor._SaveLink_Unsafe(s, caller, parent, f, parent.OrgId, false, now);

          //var location = new DocumentItemLocation() {
          //  CreateTime = now,
          //  DocumentFolderId = parent.Id,
          //  IsShortcut = false,
          //  ItemId = f.Id,
          //  ItemType = DocumentItemType.DocumentFolder,
          //  SourceOrganizationId = parent.OrgId,
          //};
          //s.Save(location);
          PermissionsAccessor.InitializePermItems_Unsafe(s, caller, PermItem.ResourceType.DocumentsFolder, f.Id, PermTiny.InheritedFromDocumentFolder(parent.Id));

          tx.Commit();
          s.Flush();

          return DocumentItemVM.Create(f, false, new DocumentItemSettings(parent));

        }
      }
    }

    public async static Task<DocumentsFolderVM> GetVisibleOrgFolders(UserOrganizationModel caller, long userId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.Self(userId);
          var user = s.Get<UserOrganizationModel>(userId);
          var userModel = s.Get<UserModel>(user.User.Id);
          var alive = userModel.UserOrganization.Where(x => x.DeleteTime == null).ToList();
          var folderIds = alive.Where(x => x.Organization.DocumentsMainFolderId != null)
                     .Select(y => y.Organization.DocumentsMainFolderId.Value)
                     .Distinct().ToList();

          var docFolders = s.QueryOver<DocumentsFolder>()
                    .Where(x => x.DeleteTime == null)
                    .WhereRestrictionOn(x => x.Id).IsIn(folderIds)
                    .Select(x => x.Id, x => x.LookupId)
                    .List<object[]>()
                    .ToDefaultDictionary(x => (long?)x[0], x => (string)x[1], x => null);

          var contents = new List<DocumentItemVM>();
          foreach (var x in alive) {
            if (x.DeleteTime == null && x.Organization.DeleteTime == null) {
              var link = new DocumentItemLinkSettings() {
                CreateTime = x.Organization.CreationTime,
                Name = x.Organization.GetName(),
                Url = "/documents/org/" + x.Organization.Id,
                ImageUrl = x.Organization.Settings.HasImage() ? x.Organization.GetImageUrl() : null,
                Target = DocumentItemWindowTarget.Default,
                Generated = true,
                CanDelete = false
              };
              var settings = new DocumentItemSettings();
              var item = DocumentItemVM.CreateLink(link, settings);
              item.Id = docFolders[x.Organization.DocumentsMainFolderId];
              contents.Add(item);
            }
          }

          foreach (var x in contents) {
            x.CanEdit = true;
          }

          //var admins = WherePermitted(s, perms, PermItem.AccessLevel.Admin, contents.Select(x => x.TinyItem).ToList()).ToLookup(x => x);
          //var edits = WherePermitted(s, perms, PermItem.AccessLevel.Edit, contents.Select(x => x.TinyItem).ToList()).ToLookup(x => x);

          //foreach (var x in contents) {
          //	x.CanAdmin = (x.CanAdmin) || admins[x.TinyItem].Any();              // e => e.ItemId == x.TinyItem.ItemId && e.ItemType == x.TinyItem.ItemType);
          //	x.CanDelete = (x.CanDelete ?? true) && admins[x.TinyItem].Any();    //admins.Any(e => e.ItemId == x.TinyItem.ItemId && e.ItemType == x.TinyItem.ItemType);
          //	x.CanEdit = (x.CanEdit) || edits[x.TinyItem].Any();                 //edits.Any(e => e.ItemId == x.TinyItem.ItemId && e.ItemType == x.TinyItem.ItemType);
          //	//x.Menu.AddRange(MenuItems.ConstructMenu(x).ToList());
          //}

          var output = DocumentsFolderVM.GenerateListing(contents, userModel.CreateTime);
          return output;

        }
      }
    }

    /// <summary>
    /// Only coaches have a coaching template folder
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="orgId"></param>
    /// <returns></returns>
    public async static Task<string> GetCoachingTemplateFolder(UserOrganizationModel caller, long orgId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewOrganization(orgId);
          var org = s.Get<OrganizationModel>(orgId);
          var save = false;
          if (org.CoachTemplateDocumentsFolderId==null) {
            await DocumentsAccessor.ForceInitializeMainOrganizationFolder_Unsafe(s, orgId);
            var o = s.Get<OrganizationModel>(orgId);
            save = true;
          }

          if (save) {
            tx.Commit();
            s.Flush();
          }

          if (org.CoachTemplateDocumentsFolderId == null)
            throw new PermissionsException("Coach folder does not exist.");

          var found = s.Get<DocumentsFolder>(org.CoachTemplateDocumentsFolderId);
          return found.LookupId;
        }
      }
    }


    public static PermissionDropdownVM GetPermItems(UserOrganizationModel caller, long itemId, DocumentItemType itemType) {
      var resourceType = ConvertDocumentItemTypeToPermResourceType(itemType);
      return PermissionsAccessor.GetPermItems(caller, itemId, resourceType);
    }

    public static PermItem.ResourceType ConvertDocumentItemTypeToPermResourceType(DocumentItemType itemType) {
      PermItem.ResourceType resourceType = PermItem.ResourceType.Invalid;
      switch (itemType) {
        case DocumentItemType.EncryptedFile:
          resourceType = PermItem.ResourceType.File;
          break;
        case DocumentItemType.DocumentFolder:
          resourceType = PermItem.ResourceType.DocumentsFolder;
          break;
        case DocumentItemType.Process:
          resourceType = PermItem.ResourceType.Process;
          break;
        case DocumentItemType.Whiteboard:
          resourceType = PermItem.ResourceType.Whiteboard;
          break;
        case DocumentItemType.VTO:
          resourceType = PermItem.ResourceType.VTO;
          break;
        default:
          break;
      }

      if (resourceType == PermItem.ResourceType.Invalid) {
        throw new PermissionsException("Cannot edit permissions.");
      }

      return resourceType;
    }

    public async static Task<string> GetMainOrganizationFolder(UserOrganizationModel caller, long forUserId, long orgId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.Self(forUserId);
          perms.TryWithAlternateUsers(x => x.ViewOrganization(orgId));
          var org = s.Get<OrganizationModel>(orgId);
          if (org.DocumentsMainFolderId == null || org.DocumentsMainFolderId <= 0) {
            var teamId = OrganizationAccessor.GetAllMembersTeamId(s, perms, orgId);
            CreateMainOrganizationFolder_Unsafe(s, caller, org, teamId);
            tx.Commit();
            s.Flush();
          }

          return s.Get<DocumentsFolder>(org.DocumentsMainFolderId.Value).LookupId;
        }
      }
    }

    public static void CreateMainOrganizationFolder_Unsafe(ISession s, UserOrganizationModel caller, OrganizationModel org, long teamId) {
      var folder = new DocumentsFolder() {
        Name = "Documents",
        OrgId = org.Id,
        Generated = true,
        Class = "root",
        LookupId = RandomUtil.SecureRandomString(16),
        IconHint = "main",
        Root = true,
        Interceptor = FolderConsts.RootFolder.Interceptor,
      };
      s.Save(folder);
      org.DocumentsMainFolderId = folder.Id;
      PermissionsAccessor.InitializePermItems_Unsafe(s, caller, PermItem.ResourceType.DocumentsFolder, folder.Id, PermTiny.RGM(teamId, true, true, false), PermTiny.Admins());
      s.Update(org);
    }

    /// <summary>
    /// This method is grossly unsafe. I'm only allowing it because it doesnt return anything.
    /// </summary>
    public static async Task ForceInitializeMainOrganizationFolder_Unsafe(ISession s, long orgId) {
      var org = s.Get<OrganizationModel>(orgId);
      //booo yuck..
      var fakeCaller = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId && x.DeleteTime == null && x.User != null).Take(1).SingleOrDefault();
      var perms = PermissionsUtility.Create(s, fakeCaller);
      if (org.DocumentsMainFolderId==null) {
        //need to create it ...
        var teamId = OrganizationAccessor.GetAllMembersTeamId(s, perms, orgId);
        CreateMainOrganizationFolder_Unsafe(s, fakeCaller, org, teamId);
      }

      //Need to force it to generate contents...
      var folder = s.Get<DocumentsFolder>(org.DocumentsMainFolderId);
      await GetFolderContents_Part1(s, perms, fakeCaller, folder.LookupId, null, null, null);
      //Dont forget to commit
    }

    public static async Task<DirectoryVM> GetRootDirectories(UserOrganizationModel caller, long forUserId) {
      var count = 1;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.Self(forUserId);
          var user = s.Get<UserOrganizationModel>(forUserId);
          count = user.User.UserOrganizationIds.Count();
        }
      }

      if (count > 1) {
        var visible = await GetVisibleOrgFolders(caller, forUserId);
        foreach (var v in visible.Contents.Where(x => x.Id != null)) {
          //turn them all into 'folders'
          v.TinyItem.ItemType = DocumentItemType.DocumentFolder;
        }
        var res = new DirectoryVM(visible);
        res.Subdirectories.ForEach(x => x.IsOrgFolder = true);
        return res;
      } else {
        var rootDir = await GetMainOrganizationFolder(caller, forUserId, caller.Organization.Id);
        var visible = await GetFolderContents(caller, rootDir);
        var res = new DirectoryVM(visible);
        res.IsOrgFolder = true;
        return res;
      }
    }

    public static async Task<DirectoryVM> GetDirectories(UserOrganizationModel caller, string folderGuid) {
      var folderVM = await GetFolderContents(caller, folderGuid);
      var subfolders = folderVM.Contents.Where(x => x.Type == DocumentItemType.DocumentFolder).ToList();
      return new DirectoryVM(folderVM.Folder) {
        Subdirectories = subfolders.Select(x => new DirectoryVM(x)).ToList()
      };
    }

    public static async Task<DocumentsFolderVM> GetFolderContents(UserOrganizationModel caller, string folderGuid, DocumentsFolderOrderType? order = null, string asc = null, DocumentsFolderDisplayType? display = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          
          var pt1 = await GetFolderContents_Part1(s, perms, caller, folderGuid, order, asc, display);
          var result = pt1.DocumentsFolderVM;
          var folder = pt1.DocumentsFolder;

          tx.Commit();
          s.Flush();

          var count = result.Contents.Count;
          result.Contents = WherePermitted(s, perms, PermItem.AccessLevel.View, result.Contents);
          var hasHidden = result.Contents.Count < count;

          var admins = WherePermitted(s, perms, PermItem.AccessLevel.Admin, result.Contents.Select(x => x.TinyItem).ToList()).ToLookup(x => x);
          var edits = WherePermitted(s, perms, PermItem.AccessLevel.Edit, result.Contents.Select(x => x.TinyItem).ToList()).ToLookup(x => x);

          foreach (var x in result.Contents) {
            x.CanAdmin = (x.CanAdmin) || admins[x.TinyItem].Any();              // e => e.ItemId == x.TinyItem.ItemId && e.ItemType == x.TinyItem.ItemType);
            x.CanDelete = (x.CanDelete ?? true) && admins[x.TinyItem].Any();    //admins.Any(e => e.ItemId == x.TinyItem.ItemId && e.ItemType == x.TinyItem.ItemType);
            x.CanEdit = (x.CanEdit) || edits[x.TinyItem].Any();                 //edits.Any(e => e.ItemId == x.TinyItem.ItemId && e.ItemType == x.TinyItem.ItemType);
            x.Menu.AddRange(MenuItems.ConstructMenu(x).ToList());
          }

          result.ContainsHiddenItems = hasHidden;

          foreach (var interceptor in FolderInterceptors) {
            if (interceptor.ShouldExecute(s, folder)) {
              await interceptor.OnAfterLoad(result);
            }
          }

          return result;
        }
      }
    }

    private class GetFolderContents_Part1Results {
      public DocumentsFolderVM DocumentsFolderVM { get; set; }
      public DocumentsFolder DocumentsFolder { get; set; }
    }

    private static async Task<GetFolderContents_Part1Results> GetFolderContents_Part1(ISession s, PermissionsUtility perms, UserOrganizationModel caller, string folderGuid, DocumentsFolderOrderType? order, string asc, DocumentsFolderDisplayType? display) {
      perms.ViewDocumentsFolder(folderGuid);
      var canEdit = perms.IsPermitted(x => x.EditDocumentsFolder(folderGuid));
      var canAdmin = perms.IsPermitted(x => x.AdminDocumentsFolder(folderGuid));
      var folder = GetDocumentFolder_Unsafe(s, folderGuid);

      //Run interceptors
      var any = false;
      foreach (var interceptor in FolderInterceptors) {
        if (interceptor.ShouldExecute(s, folder)) {
          await interceptor.OnBeforeLoad(s, caller, folder);
          any = true;
        }
      }

      if (!string.IsNullOrWhiteSpace(folder.Interceptor) && !any) {
        int a = 0;//You've forgotten to register your interceptor.
      }


      if (folder.DeleteTime != null)
        throw new PermissionsException("Folder was deleted.");

      var folderOrg = s.Get<OrganizationModel>(folder.OrgId);
      if (folderOrg.DeleteTime != null)
        throw new PermissionsException("Organization no longer exists");

      var mainFolderId = folderOrg.DocumentsMainFolderId;
      var directParentFolderQ = GetParentFolder_Future(s, folder.Id);

      //Heavy Lifting
      var contents = GetDocumentItemVMs_Unsafe(s, folder, x => Criterions.GetItemIdsInFolder(folder.Id, x));

      var includeListing = caller.User.UserOrganizationIds.Count() > 1;

      var parents = directParentFolderQ.ToList();
      var parentFolderName = parents.Any() ? parents.First().Name : (string)null;
      var path = await GetPath(s, perms, folder.Id, includeListing, folderOrg.GetName());
      var isMain = mainFolderId == folder.Id;

      var parent = path.LastOrDefault();

      bool? ascending = (asc == null ? null : (bool?)(asc == "asc"));

      var result = new DocumentsFolderVM {
        Contents = contents.ToList(),
        Folder = DocumentItemVM.Create(folder, folder.Id == mainFolderId, new DocumentItemSettings(parent)),
        Path = path,
        OrderType = order ?? folder.OrderType,
        DisplayType = display ?? folder.DisplayType,
        OrderAscending = ascending ?? folder.OrderAscending,
        TimeSettings = caller.GetTimeSettings(),
      };


      if (canEdit) {
        result.Folder.Menu.AddRange(new[]{
              new DocumentItemMenuItemVM("Upload file", "Documents.upload()"),
              new DocumentItemMenuItemVM("Upload live file", "Documents.uploadLive()"),
              new DocumentItemMenuItemVM("New folder", "Documents.newFolder()"),
              DocumentItemMenuItemVM.CreateSeparator()
            });
      }
      result.Folder.CanEdit = canEdit;
      result.Folder.CanAdmin = canAdmin;
      result.Folder.CanDelete = folder.CanDelete && result.Folder.CanAdmin;

      result.Folder.Menu.AddRange(new[]{
            DocumentItemMenuItemVM.Create(path.Count>1, "Go up a level","Documents.goUp()"),
            new DocumentItemMenuItemVM("Go home","Documents.goHome()"),
              DocumentItemMenuItemVM.CreateSeparator(),
            MenuItems.Permissions(result.Folder),
          });




      List<TinyDocumentItem> favorites = null;
      List<TinyDocumentItem> recent = null;

      if (isMain) {
        var allInteresting = s.QueryOver<DocumentItemLookupCache>()
          .Where(x => x.DeleteTime == null && x.ForUser == caller.Id)
          .Where(x => x.CacheKind == DocumentItemLookupCacheKind.Favorite || (x.CacheKind == DocumentItemLookupCacheKind.Recent && x.CreateTime > DateTime.UtcNow.AddDays(-90)))
          .OrderBy(x => x.CreateTime).Desc
          .Select(x => x.ItemId, x => x.ItemType, x => x.CacheKind)
          .List<object[]>()
          .Select(x => new {
            ItemId = (long)x[0],
            ItemType = (DocumentItemType)x[1],
            CacheKind = (DocumentItemLookupCacheKind)x[2]
          }).ToList();

        favorites = allInteresting.Where(x => x.CacheKind == DocumentItemLookupCacheKind.Favorite)
                  .Select(f => new TinyDocumentItem(f.ItemId, f.ItemType))
                  .ToList();
        recent = allInteresting.Where(x => x.CacheKind == DocumentItemLookupCacheKind.Recent)
                  .Take(9)
                  .Select(f => new TinyDocumentItem(f.ItemId, f.ItemType))
                  .ToList();

        result.ShowFavorites = true;
        result.ShowRecent = true;
      }

      //Don't .ToList() until all items are loaded.
      var favoritesItems = GetItems_Unsafe(s, folder, favorites);
      var recentItems = GetItems_Unsafe(s, folder, recent);

      result.Favorites = favoritesItems.ToList();
      result.Recent = recentItems.ToList();

      foreach (var interceptor in FolderInterceptors) {
        if (interceptor.ShouldExecute(s, folder)) {
          try {
            var items = await interceptor.GeneratedDocumentItems(s, caller, folder);
            if (items != null) {
              result.Contents.AddRange(items);
            }
          } catch (Exception e) {
          }
        }
      }

      var allHeadingGroupNames = result.Contents.SelectMany(x => x.HeadingGroupNames ?? new List<string>()).Distinct().ToList();
      result.HeadingGroups = new List<DocumentHeadingGroup>();

      foreach (var hgn in allHeadingGroupNames) {
        result.HeadingGroups.Add(new DocumentHeadingGroup() {
          HeadingName = hgn,
          Contents = result.Contents.Where(x => x.HeadingGroupNames.Contains(hgn)).ToList()
        });
        result.ShowHeadingGroups = true;
      }
      result.Contents.RemoveAll(x => x.HeadingGroupNames.Any());


      if (canEdit) {
        var anyUpdate = false;
        if (order != null && folder.OrderType != order) {
          folder.OrderType = order;
          anyUpdate = true;
        }
        if (display != null && folder.DisplayType != display) {
          folder.DisplayType = display;
          anyUpdate = true;
        }
        if (ascending != null && folder.OrderAscending != ascending) {
          folder.OrderAscending = ascending.Value;
          anyUpdate = true;
        }
        if (anyUpdate) {
          s.Update(folder);
        }
      }

      return new GetFolderContents_Part1Results {
        DocumentsFolderVM = result,
        DocumentsFolder = folder
      };
    }

    public static DocumentsFolder GetDocumentsFolder(UserOrganizationModel caller, string folderGuid) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetDocumentsFolder(s, perms, folderGuid);
        }
      }
    }

    public static DocumentsFolder GetDocumentsFolder(ISession s, PermissionsUtility perms, string folderGuid) {
      perms.ViewDocumentsFolder(folderGuid);
      return GetDocumentFolder_Unsafe(s, folderGuid);
    }

    private static DocumentsFolder GetDocumentFolder_Unsafe(ISession s, string folderGuid) {
      var f = s.QueryOver<DocumentsFolder>().Where(x => x.LookupId == folderGuid).Take(1).SingleOrDefault();
      return f;
    }
    private static long GetDocumentFolderId_Unsafe(ISession s, string folderGuid) {
      var f = s.QueryOver<DocumentsFolder>().Where(x => x.LookupId == folderGuid).Select(x => x.Id).Take(1).SingleOrDefault<long>();
      if (f == 0)
        throw new PermissionsException("Folder not found.");
      return f;
    }


    [Obsolete("Unsafe")]
    private static DocumentItemPathVM GetFolderPathItem_Unsafe(ISession s, long folderId) {
      var folder = s.Get<DocumentsFolder>(folderId);
      var parent = GetParentFolder_Future(s, folderId).FirstOrDefault();

      var r = new DocumentItemPathVM {
        Id = folder.LookupId,
        Name = folder.Name,
        FolderId = folder.Id,
        Url = "/documents/folder/" + folder.LookupId,
        ParentFolderId = parent.NotNull(x => (long?)x.Id),
        //FolderLookupId = folder.LookupId,
        //ParentFolderLookupId = parent.NotNull(x => x.LookupId),
      };
      return r;
    }






  }
}