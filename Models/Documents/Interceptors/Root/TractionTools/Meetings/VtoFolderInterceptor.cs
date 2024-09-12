using NHibernate;
using RadialReview.Models.Documents.Interceptors.Data;
using RadialReview.Models.Downloads;
using RadialReview.Models.L10;
using RadialReview.Models.VTO;
using RadialReview.Utilities.Files;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Models.Documents.Interceptors {
  public class VtoFolderInterceptor : IDocumentFolderInterceptor_Unsafe {
    public bool ShouldExecute(ISession s, DocumentsFolder folder) {
      return FolderConsts.VtoFolder.InterceptorMatches(folder);
    }

    public async Task OnBeforeLoad(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {
      //noop
    }
    public async Task OnAfterLoad(DocumentsFolderVM folderVM) {
      //noop
    }


    public async Task<List<DocumentItemVM>> GeneratedDocumentItems(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {

      var recurrenceId = folder.GetInterceptorData<MeetingFolderData>().RecurrenceId;
      var recur = s.Get<L10Recurrence>(recurrenceId);


      var files = s.QueryOver<EncryptedFileModel>()
        .Where(x => x.DeleteTime == null && x.ParentModel == ForModel.Create<VtoModel>(recur.VtoId))
        .List()
        .ToList();

      var format = s.Get<OrganizationModel>(folder.OrgId).GetTimeSettings().DateFormat ?? "M/d/yyyy";

      if (folder.DisplayType == null && folder.OrderType == null) {
        folder.OrderAscending = false;
        folder.DisplayType = Enums.DocumentsFolderDisplayType.Grouped;
        folder.OrderType = Enums.DocumentsFolderOrderType.Created;
        s.Update(folder);
      }


      var dedup = FileNameUtility.CreateNameDeduplicator();
      return files.OrderBy(x => x.CreateTime).Select(x => {
        var r = DocumentItemVM.Create(x, new DocumentItemSettings(folder));
        r.Name = dedup.AdjustName(x.CreateTime.ToString(format));
        return r;
      }).OrderByDescending(x => x.CreateTime).ToList();
    }

    public async Task OnAfterLink(ISession s, UserOrganizationModel caller, DocumentsFolder folder, DocumentItemLocation location) {
      //noop
    }
    public async Task OnAfterUnlink(ISession s, UserOrganizationModel caller, DocumentsFolder fromFolder, DocumentItemLocation location) {
      //noop
    }
  }
}

