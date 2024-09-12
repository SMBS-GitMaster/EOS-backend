using NHibernate;
using RadialReview.Models.Documents.Interceptors.Data;
using RadialReview.Models.L10;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Models.Documents.Interceptors {
  public class MeetingSummaryFolderInterceptor : IDocumentFolderInterceptor_Unsafe {
    public async Task<List<DocumentItemVM>> GeneratedDocumentItems(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {

      var recurrenceId = folder.GetInterceptorData<MeetingFolderData>().RecurrenceId;

      var meetings = s.QueryOver<L10Meeting>()
        .Where(x => x.DeleteTime == null && x.L10RecurrenceId == recurrenceId)
        .Select(x => x.Id, x => x.CreateTime)
        .List<object[]>()
        .Select(x => new {
          MeetingId = (long)x[0],
          StartTime = (DateTime)x[1]
        }).ToList();

      var numbers = new DefaultDictionary<string, int>(x => 0);
      var format = s.Get<OrganizationModel>(folder.OrgId).GetTimeSettings().DateFormat ?? "M/d/yyyy";

      var dedup = FileNameUtility.CreateNameDeduplicator();

      folder.OrderType = Enums.DocumentsFolderOrderType.Created;
      folder.DisplayType = Enums.DocumentsFolderDisplayType.Grouped;
      folder.OrderAscending = false;

      return meetings.OrderBy(x => x.StartTime).Select(x => {
        var name = dedup.AdjustName(x.StartTime.ToString(format));
        var link = new DocumentItemLinkSettings() {
          CreateTime = x.StartTime,
          Name = name,
          Url = "/L10/MeetingSummary/" + x.MeetingId,
          Generated = true,
          CanDelete = false
        };
        return DocumentItemVM.CreateLink(link, new DocumentItemSettings(folder));
      }).OrderByDescending(x => x.CreateTime).ToList();

    }

    public async Task OnBeforeLoad(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {
      //noop
    }
    public async Task OnAfterLoad(DocumentsFolderVM folderVM) {
      //noop
    }

    public bool ShouldExecute(ISession s, DocumentsFolder folder) {
      return FolderConsts.MeetingSummaryFolder.InterceptorMatches(folder);
    }

    public async Task OnAfterLink(ISession s, UserOrganizationModel caller, DocumentsFolder folder, DocumentItemLocation location) {
      //noop
    }
    public async Task OnAfterUnlink(ISession s, UserOrganizationModel caller, DocumentsFolder fromFolder, DocumentItemLocation location) {
      //noop
    }
  }
}