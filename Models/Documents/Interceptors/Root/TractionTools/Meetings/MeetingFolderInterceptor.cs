using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models.Documents.Interceptors.Data;
using RadialReview.Models.L10;
using RadialReview.Variables;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Models.Documents.Interceptors {
  public class MeetingFolderInterceptor : IDocumentFolderInterceptor_Unsafe {

    public async Task OnBeforeLoad(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {
      var subfolders = s.QueryOver<DocumentsFolder>()
                .WithSubquery.WhereProperty(x => x.Id)
                .In(DocumentsAccessor.Criterions.GetItemIdsInFolder_IncludingDeletedItems(folder.Id, DocumentItemType.DocumentFolder))
                .List().ToList();

      var meetingFolderStructure = s.GetSettingOrDefault(Variable.Names.FOLDER_MEETING, () => {
        return new List<GS>(){
            GS.Historical("Business Plan", FolderConsts.VtoFolder),
            GS.Historical("Quarterly Printouts", FolderConsts.QuarterlyPrintoutFolder),
            GS.Historical("Meeting Summaries", FolderConsts.MeetingSummaryFolder),
            GS.Historical("Whiteboards", FolderConsts.MeetingWhiteboardFolder),
            GS.AutoPopFolder("Notes", FolderConsts.NotesFolder,"Notes",null,false),
        };
      });

      var recurId = folder.GetInterceptorData<MeetingFolderData>().RecurrenceId;
      var recur = s.Get<L10Recurrence>(recurId);

      var ar = SetUtility.AddRemove(subfolders.Where(x => x.Generated), x => x.Class, meetingFolderStructure, x => x.Class);

      foreach (var a in ar.AddedValues) {
        var f = DocumentsFolder.CreateFrom(a, recur.OrganizationId, new MeetingFolderData() { RecurrenceId = recurId, OrgId = folder.OrgId });
        f.CreateTime = recur.CreateTime;
        f.CreatorId = recur.CreatedById;
        s.Save(f);

        await DocumentsAccessor._SaveLink_Unsafe(s, caller, folder, f, folder.OrgId, false);
        //var link = DocumentItemLocation.CreateFrom(folder, f);
        //s.Save(link);
        var creator = s.Get<UserOrganizationModel>(recur.CreatedById);
        PermissionsAccessor.InitializePermItems_Unsafe(s, creator, PermItem.ResourceType.DocumentsFolder, f.Id, PermTiny.InheritedFromL10Recurrence(recurId));
      }

    }
    public async Task OnAfterLoad(DocumentsFolderVM folderVM) {
      //noop
    }

    public bool ShouldExecute(ISession s, DocumentsFolder folder) {
      return FolderConsts.MeetingFolder.InterceptorMatches(folder);
    }


    public async Task<List<DocumentItemVM>> GeneratedDocumentItems(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {
      var data = folder.GetInterceptorData<MeetingFolderData>();
      var recurId = data.RecurrenceId;
      var recur = s.Get<L10Recurrence>(recurId);
      var items = new List<DocumentItemVM>();
      if (recur.DeleteTime == null) {
        items.Add(DocumentItemVM.CreateApplicationLink(
              "Go to " + (recur.Name ?? "this meeting"),
              "/L10/meeting/" + recur.Id,
              recur.MeetingType == MeetingType.SamePage ? "SPM" : "L10",
              new DocumentItemSettings(folder)));
        items.Add(DocumentItemVM.CreateApplicationLink(
                            "View Business Plan for " + (recur.Name ?? "this meeting"),
              "/vto/edit/" + recur.VtoId,
                            "Business Plan",
              new DocumentItemSettings(folder)));
        if (recur.WhiteboardId != null) {
          items.Add(DocumentItemVM.CreateApplicationLink(
            "Whiteboard for " + (recur.Name ?? "this meeting"),
            "/whiteboard/edit/" + recur.WhiteboardId,
            "WB",
            new DocumentItemSettings(folder)));
        }
      }
      return items;
    }


    public async Task OnAfterLink(ISession s, UserOrganizationModel caller, DocumentsFolder folder, DocumentItemLocation location) {
      //noop
    }
    public async Task OnAfterUnlink(ISession s, UserOrganizationModel caller, DocumentsFolder fromFolder, DocumentItemLocation location) {
      //noop
    }
  }
}