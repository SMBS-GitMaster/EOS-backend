using Microsoft.AspNetCore.Html;
using Newtonsoft.Json;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models.Documents.Interceptors.Data;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Models.Documents.Interceptors {
  public class CoachTemplateFolderInterceptor : IDocumentFolderInterceptor_Unsafe {
    public async Task<List<DocumentItemVM>> GeneratedDocumentItems(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {
      var orgs = GetCoachedOrgs(s, folder, caller.User.Id);

      foreach (var o in orgs) {
        if (o.CoachDocumentsFolderId==null) {
          //this method edits the org models in memory.. its magic!
          await DocumentsAccessor.ForceInitializeMainOrganizationFolder_Unsafe(s, o.Id);
        }
      }


      var lookup = orgs.Select(x => new {
        name = x.Name,
        folderId = x.CoachDocumentsFolderId
      }).ToDictionary(x => x.folderId, x => x.name);

      var folderIds = orgs.Select(x => x.CoachDocumentsFolderId).ToArray();

      var folders = s.QueryOver<DocumentsFolder>()
        .Where(x => x.DeleteTime==null)
        .WhereRestrictionOn(x => x.Id).IsIn(folderIds)
        .List().ToList();

      return folders.Select(x => {
        var doc = DocumentItemVM.Create(x, false, new DocumentItemSettings(folder));
        doc.Name = lookup[x.Id];
        doc.HeadingGroupNames.Add("Client Folders");
        return doc;
      }).ToList();




    }

    public async Task OnBeforeLoad(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {
      //noop
    }
    public async Task OnAfterLoad(DocumentsFolderVM folderVM) {
      folderVM.MainSectionTitle = "Files Shared With All Clients";
      folderVM.SetInstructionBar("<img src='/Content/Documents/Icons/whistle.svg'/>This is a special folder to share documents with your clients. Anything uploaded to this folder will appear in all your clients' shared folder. If you wish to share a file with just one client, you can upload it to their individual folder.");
      folderVM.ExecuteOnLoad = "Upload.templateMode=true;";
    }

    public bool ShouldExecute(ISession s, DocumentsFolder folder) {
      return FolderConsts.CoachTemplateFolder.InterceptorMatches(folder);
    }

    public async Task OnAfterLink(ISession s, UserOrganizationModel caller, DocumentsFolder folder, DocumentItemLocation location) {
      List<OrganizationModel> orgs = GetCoachedOrgs(s, folder, caller.User.Id);

      foreach (var o in orgs) {
        try {
          var clientsCoachDocumentsFolderId = o.CoachDocumentsFolderId;
          if (clientsCoachDocumentsFolderId==null) {
            //this method edits the org in memory ..
            await DocumentsAccessor.ForceInitializeMainOrganizationFolder_Unsafe(s, o.Id);
            clientsCoachDocumentsFolderId = o.CoachDocumentsFolderId;
          }

          var clientsCoachFolder = s.Get<DocumentsFolder>(clientsCoachDocumentsFolderId);

          await _CopyItemToClient(s, caller, location.ItemType, location.ItemId, location.CreateTime, o.Id, clientsCoachDocumentsFolderId.Value, clientsCoachFolder);

        } catch (Exception e) {
          Debug.WriteStackTrace("Failed to link coach document to client"+folder.Id +" "+o.Id);
        }
      }
    }
    public async Task OnAfterUnlink(ISession s, UserOrganizationModel coachUOM, DocumentsFolder fromFolder, DocumentItemLocation location) {
      List<OrganizationModel> orgs = GetCoachedOrgs(s, fromFolder, coachUOM.User.Id);
      var now = DateTime.UtcNow;

      foreach (var o in orgs) {
        try {
          var clientsCoachDocumentsFolderId = o.CoachDocumentsFolderId;
          if (clientsCoachDocumentsFolderId==null) {
            //this method edits the org in memory ..
            await DocumentsAccessor.ForceInitializeMainOrganizationFolder_Unsafe(s, o.Id);
            clientsCoachDocumentsFolderId = o.CoachDocumentsFolderId;
          }

          var clientsCoachFolder = s.Get<DocumentsFolder>(clientsCoachDocumentsFolderId);


          var found = s.QueryOver<DocumentItemLocation>()
           .Where(x => x.DeleteTime == null && x.DocumentFolderId == clientsCoachDocumentsFolderId && x.ItemId == location.ItemId && x.ItemType == location.ItemType)
           .Take(1).SingleOrDefault();

          //await _CopyItemToClient(s, caller, location.ItemType, location.ItemId, location.CreateTime, o.Id, clientsCoachDocumentsFolderId.Value, clientsCoachFolder);
          if (found!=null) {
            await DocumentsAccessor._DeleteLink_Unsafe(s, coachUOM, clientsCoachFolder, found, now);
          }
        } catch (Exception e) {
          Debug.WriteStackTrace("Failed to unlink coach document to client"+fromFolder.Id +" "+o.Id);
        }
      }
    }

    private static async Task _CopyItemToClient(ISession s, UserOrganizationModel coachUOM, DocumentItemType itemType, long itemId, DateTime itemCreateTime, long clientOrgId, long clientsCoachDocumentsFolderId, DocumentsFolder clientsCoachFolder) {
      await DocumentsAccessor._SaveLink_Unsafe(s, coachUOM, clientsCoachFolder, itemId, itemType, clientOrgId, true, itemCreateTime);
      var permResourceType = DocumentsAccessor.ConvertDocumentItemTypeToPermResourceType(itemType);
      PermissionsAccessor.InitializePermItems_Unsafe(s, coachUOM, permResourceType, itemId, PermTiny.InheritedFromDocumentFolder(clientsCoachDocumentsFolderId, true, false, false));
    }

    public static async Task LinkAllFilesToAccount_Unsafe(ISession s, string coachId, long clientOrgId) {
      OrganizationModel orgAlias = null;
      var possibleCoaches = s.QueryOver<UserOrganizationModel>()
        .JoinAlias(x => x.Organization, () => orgAlias)
        .Where(x => x.User.Id== coachId && x.DeleteTime == null && orgAlias.DeleteTime==null &&
                    (orgAlias.AccountType == AccountType.Coach || orgAlias.AccountType == AccountType.Implementer || orgAlias.AccountType == AccountType.BloomGrowthCoach)
        ).List().ToList();

      if (possibleCoaches.Count != 1) {
        Debug.WriteLine("[CoachTemplates] Could not figure out which account to attach the coach to. Skipping file copy.");
        return;
      }
      var coachUOM = possibleCoaches.Single();
      var coachOrgId = coachUOM.Organization.Id;
      var o = s.Get<OrganizationModel>(clientOrgId);

      var clientsCoachDocumentsFolderId = o.CoachDocumentsFolderId;
      if (clientsCoachDocumentsFolderId==null) {
        //this method edits the org in memory ..
        await DocumentsAccessor.ForceInitializeMainOrganizationFolder_Unsafe(s, o.Id);
        clientsCoachDocumentsFolderId = o.CoachDocumentsFolderId;
      }

      var possibleCoachTemplateFolders = s.QueryOver<DocumentsFolder>()
        .Where(x => x.DeleteTime == null && x.OrgId == coachOrgId && x.Class== FolderConsts.CoachTemplateFolder.Class)
        .List().ToList();

      if (possibleCoachTemplateFolders.Count>1) {
        Debug.WriteLine("[CoachTemplates] Too many coach template folders. Skipping file copy.");
        return;
      } else if (possibleCoachTemplateFolders.Count==0) {
        Debug.WriteLine("[CoachTemplates] No coach template folders. Skipping file copy.");
        return;
      }

      var coachTemplateFolder = possibleCoachTemplateFolders.Single();

      var coachFolderItems = s.QueryOver<DocumentItemLocation>()
        .Where(x => x.DeleteTime==null && x.DocumentFolderId == coachTemplateFolder.Id)
        .List().ToList();

      var clientsCoachFolder = s.Get<DocumentsFolder>(clientsCoachDocumentsFolderId);

      foreach (var item in coachFolderItems) {
        await _CopyItemToClient(s, coachUOM, item.ItemType, item.ItemId, item.CreateTime, clientOrgId, clientsCoachDocumentsFolderId.Value, clientsCoachFolder);
      }



    }




    private static List<OrganizationModel> GetCoachedOrgs(ISession s, DocumentsFolder folder, string userId) {
      var possibleCoachUserOrgIds = s.QueryOver<UserOrganizationModel>()
              .Where(x => x.DeleteTime == null && x.Organization.Id == folder.OrgId && x.IsRadialAdmin == false)
              .Select(x => x.User.Id)
              .List<string>()
              .ToList();

      var coachOrgs = s.QueryOver<CoachOrg>()
        .Where(x => x.DeleteTime == null && x.CoachId == userId)
        .List().ToList();

      var coachOrg_OrgIds = coachOrgs.Select(x => x.OrgId).ToArray();

      var orgs = s.QueryOver<OrganizationModel>()
        .Where(x => x.DeleteTime == null)
        .WhereRestrictionOn(x => x.Id).IsIn(coachOrg_OrgIds)
        .List().ToList();
      return orgs;
    }

  }
}
