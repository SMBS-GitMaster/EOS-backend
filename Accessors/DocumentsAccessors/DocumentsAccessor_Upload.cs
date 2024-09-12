using RadialReview.Models;
using RadialReview.Models.Documents;
using RadialReview.Models.Downloads;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadialReview.Models.Documents.Interceptors.Data;
using System.IO;
using RadialReview.Utilities.FileTypes;
using RadialReview.Middleware.Services.BlobStorageProvider;
using NHibernate;

namespace RadialReview.Accessors {
  public partial class DocumentsAccessor {

    public static async Task<DocumentItemVM> Upload(UserOrganizationModel caller, IBlobStorageProvider bsp, string name, string type, Stream stream, string folderId) {
      DocumentsFolder folder;
      FileTypeExtensionUtility.FileType fileType;
      string tagName;
      var now = DateTime.UtcNow;

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.CreateFileUnderDocumentsFolder(folderId);
          folder = GetDocumentFolder_Unsafe(s, folderId);
          fileType = FileTypeExtensionUtility.GetFileTypeFromExtension(type);
          tagName = folder.GetInterceptorProperty(InterceptConstants.TAG_HINTS, (string)null);

        }
      }
      var tags = new List<TagModel>();
      if (tagName != null) {
        tags = tagName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => TagModel.Create(x)).ToList();
      }

      var permtypes = new PermTiny[]{
            PermTiny.Creator(),
            PermTiny.InheritedFrom(PermItem.ResourceType.DocumentsFolder,folder.Id)
          };

      var fileId = await FileAccessor.Save_Unsafe(bsp, caller.Id, stream, name, type,
        fileType.Kind + ", uploaded by " + caller.GetName(),
        FileOrigin.Uploaded, FileOutputMethod.Save, ForModel.Create<DocumentsFolder>(folder.Id),
        FileNotification.DoNotNotify(), permtypes, tags.ToArray()
      );

      DocumentItemVM res;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.CanViewFile(fileId);

          var file = s.Get<EncryptedFileModel>(fileId);
          await DocumentsAccessor._SaveLink_Unsafe(s, caller, folder, file, folder.OrgId, false, now);

          tx.Commit();
          s.Flush();

          res = DocumentItemVM.Create(file, new DocumentItemSettings(folder));
          res.Menu.AddRange(MenuItems.ConstructMenu(res));

        }
      }
      return res;
    }
  }
}