using NHibernate;
using RadialReview.Models.Downloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Models.Documents.Interceptors {
  public interface IDocumentFolderInterceptor_Unsafe {
    bool ShouldExecute(ISession s, DocumentsFolder folder);


    //Used to perminently inject new items into the folder
    Task OnBeforeLoad(ISession s, UserOrganizationModel caller, DocumentsFolder folder);
    Task OnAfterLoad(DocumentsFolderVM folderVM);

    //Used to dynamically inject items into the folder
    Task<List<DocumentItemVM>> GeneratedDocumentItems(ISession s, UserOrganizationModel caller, DocumentsFolder folder);
    Task OnAfterLink(ISession s, UserOrganizationModel caller, DocumentsFolder toFolder, DocumentItemLocation location);
    Task OnAfterUnlink(ISession s, UserOrganizationModel caller, DocumentsFolder fromFolder, DocumentItemLocation location);
  }
}
