using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models.Documents.Interceptors.Data;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Models.Documents.Interceptors {


  /// <summary>
  /// Not sure if we actually need this yet.
  /// </summary>
  public class CoachClientFolderInterceptor : IDocumentFolderInterceptor_Unsafe {

    public async Task<List<DocumentItemVM>> GeneratedDocumentItems(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {
      return new List<DocumentItemVM>();
    }

    public async Task OnBeforeLoad(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {
      // var coachOrgs = s.QueryOver<CoachOrg>().Where(x => x.DeleteTime == null && ).List().ToList();

    }
    public async Task OnAfterLoad(DocumentsFolderVM folderVM) {
      //noop
      folderVM.SetInstructionBar("<img src='/Content/Documents/Icons/whistle.svg'/>This folder contains documents is shared between your organization and your coach.");
      //folderVM.Contents.Where(x=>x.
    }

    public bool ShouldExecute(ISession s, DocumentsFolder folder) {
      return FolderConsts.CoachClientFolder.InterceptorMatches(folder);
    }

    public async Task OnAfterLink(ISession s, UserOrganizationModel caller, DocumentsFolder folder, DocumentItemLocation location) {
      //noop
      var org = s.Get<OrganizationModel>(folder.OrgId);
      org.HasCoachDocuments = true;
      s.Update(org);
    }

    public async Task OnAfterUnlink(ISession s, UserOrganizationModel caller, DocumentsFolder fromFolder, DocumentItemLocation location) {
      //noop
    }
  }
}
