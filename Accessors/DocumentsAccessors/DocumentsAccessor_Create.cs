using RadialReview.Models;
using RadialReview.Models.Documents;
using RadialReview.Utilities;
using System;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
  public partial class DocumentsAccessor {

    public static async Task<WhiteboardModel> CreateWhiteboard(UserOrganizationModel caller, string name, string folderId, string svg = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var folder = DocumentsAccessor.GetDocumentsFolder(s, perms, folderId);
          var wb = await WhiteboardAccessor.CreateWhiteboard(s, perms, name, folder.OrgId, PermTiny.InheritedFromDocumentFolder(folder.Id));

          if (!string.IsNullOrWhiteSpace(svg)) {
            WhiteboardAccessor.SetBackgroundSvg(s, perms, wb.LookupId, svg);
          }


          await DocumentsAccessor._SaveLink_Unsafe(s, caller, folder, wb, folder.OrgId, false);
          //var location = new DocumentItemLocation() {
          //  CreateTime = DateTime.UtcNow,
          //  DocumentFolderId = folder.Id,
          //  IsShortcut = false,
          //  ItemId = wb.Id,
          //  ItemType = DocumentItemType.Whiteboard,
          //  SourceOrganizationId = folder.OrgId,
          //};
          //s.Save(location);

          tx.Commit();
          s.Flush();
          return wb;
        }
      }
    }
  }
}