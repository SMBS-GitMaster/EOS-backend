using NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Core.Accessors;
using RadialReview.Core.Models.Terms;
using RadialReview.Models.Documents.Enums;
using RadialReview.Models.Documents.Interceptors.Data;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Models.Documents.Interceptors.Root.TractionTools.PeopleTools {
  public class PeopleToolsManagerInterceptor : IDocumentFolderInterceptor_Unsafe {
    public async Task<List<DocumentItemVM>> GeneratedDocumentItems(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {
      var output = new List<DocumentItemVM>();

      if (PermissionsAccessor.IsPermitted(caller, x => x.CreateQuarterlyConversation(folder.OrgId))) {

        var terms = TermsAccessor.GetTermsCollection(s, PermissionsUtility.Create(s, caller), folder.OrgId);
        output.Add(DocumentItemVM.CreateApplicationLink("Issue "+terms.GetTerm(TermKey.Quarterly1_1), "/people/quarterlyconversation/issue", "", new DocumentItemSettings(folder)));

        //Add Quarterly Conversations we manage
        var items = SurveyAccessor.GetSurveyContainersBy(caller, caller, SurveyType.QuarterlyConversation).OrderByDescending(x => x.IssueDate);
        var printouts = items.Where(x => caller.UserIds.Contains(x.IssuedBy.Id))
              .Select(x => {
                var res = DocumentItemVM.CreateLink(new DocumentItemLinkSettings() {
                  CreateTime = x.CreateTime,
                  Name = x.Name + " Results",
                  Url = "/People/QuarterlyConversation/PrintAll?surveyContainerId=" + x.Id,
                  Target = DocumentItemWindowTarget.NewWindow,
                  Generated = true,
                  IconHint = "PDF",
                  CanDelete = false
                }, new DocumentItemSettings(folder));
                res.OuterClass = DocumentItemVM.GetOuterCssClass(DocumentItemType.EncryptedFile);
                res.IconClass = DocumentItemVM.GetItemIconCssClass(DocumentItemType.EncryptedFile);
                return res;
              }).ToList();
        output.AddRange(printouts);
      }
      return output;
    }

    public async Task OnAfterLink(ISession s, UserOrganizationModel caller, DocumentsFolder toFolder, DocumentItemLocation location)
    {
      //noop
    }

    public async Task OnAfterLoad(DocumentsFolderVM folderVM) {
      //noop
    }

    public async Task OnAfterUnlink(ISession s, UserOrganizationModel caller, DocumentsFolder fromFolder, DocumentItemLocation location) {
      //noop
    }

    public async Task OnBeforeLoad(ISession s, DocumentsFolder folder) {
      //noop
    }

    public async Task OnBeforeLoad(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {
      //noop
    }

    public bool ShouldExecute(ISession s, DocumentsFolder folder) {
      return FolderConsts.PeopleToolsManagerFolder.InterceptorMatches(folder);
    }
  }
}