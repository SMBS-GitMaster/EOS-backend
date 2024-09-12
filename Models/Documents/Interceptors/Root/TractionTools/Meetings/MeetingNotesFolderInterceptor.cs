using NHibernate;
using RadialReview.Models.Documents.Interceptors.Data;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using RadialReview.Utilities.Files;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace RadialReview.Models.Documents.Interceptors {
  public class NotesFolderInterceptor : IDocumentFolderInterceptor_Unsafe {
    public bool ShouldExecute(ISession s, DocumentsFolder folder) {
      return FolderConsts.NotesFolder.InterceptorMatches(folder);
    }

    public async Task OnBeforeLoad(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {
      //noop
    }
    public async Task OnAfterLoad(DocumentsFolderVM folderVM) {
      //noop
    }


    public async Task<List<DocumentItemVM>> GeneratedDocumentItems(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {
      var data = folder.GetInterceptorData<MeetingFolderData>();
      var recurId = data.RecurrenceId;
      var recur = s.Get<L10Recurrence>(recurId);
      if (recur.DeleteTime == null && PermissionsUtility.Create(s, caller).IsPermitted(x => x.ViewL10Recurrence(recurId))) {
        var notes = s.QueryOver<L10Note>().Where(x => x.Recurrence.Id == recurId && x.DeleteTime == null).List().ToList();
        var dedup = FileNameUtility.CreateNameDeduplicator();
        return notes.Select(x =>
          DocumentItemVM.CreateLink(new DocumentItemLinkSettings() {
            Url = Config.NotesUrl("/p/" + x.PadId),
            CreateTime = recur.CreateTime,
            Name = dedup.AdjustName(x.Name ?? "untitled"),
            Generated = false,
            CanDelete = false
          }, new DocumentItemSettings(folder))
        ).ToList();
      }
      return new List<DocumentItemVM>();

    }

    public async Task OnAfterLink(ISession s, UserOrganizationModel caller, DocumentsFolder folder, DocumentItemLocation location) {
      //noop
    }
    public async Task OnAfterUnlink(ISession s, UserOrganizationModel caller, DocumentsFolder fromFolder, DocumentItemLocation location) {
      //noop
    }
  }
}

