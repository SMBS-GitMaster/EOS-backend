using NHibernate;
using RadialReview.Models.Documents.Interceptors.Data;
using RadialReview.Variables;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Models.Documents.Interceptors.Root {
  public class EosToolsFolderInterceptor : IDocumentFolderInterceptor_Unsafe {

    public class EosFiles {
      public string Name { get; set; }
      public string Description { get; set; }
      public string Url { get; set; }
      public string SvgUrl { get; set; }
      public bool ImplementerOnly { get; set; }
    }

    public class ShowFilesOverride {
      public bool AllEnabled { get; set; }
      public bool AllDisabled { get; set; }
    }


    public async Task<List<DocumentItemVM>> GeneratedDocumentItems(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {
      var globalOverride = s.GetSettingOrDefault(Variable.Names.FOLDER_EOSTOOLS_FILES_OVERRIDE_DISABLE, new ShowFilesOverride() {
        AllDisabled = false,
        AllEnabled = false,
      });

      if (globalOverride.AllDisabled) {
        return new List<DocumentItemVM>();
      }

      var hasImplementer = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null && x.Organization.Id == folder.OrgId && x.IsImplementer).RowCount() > 0;

      var showTools = globalOverride.AllEnabled;
      showTools = showTools || s.GetSwitch(x => x.ShowEosToolsForOrg(folder.OrgId));
      showTools = showTools || hasImplementer;


      if (showTools) {
        var baseUrl = "https://s3.amazonaws.com/Radial/EosTools/";
        var gen = s.GetSettingOrDefault(Variable.Names.FOLDER_EOSTOOLS_FILES, new List<EosFiles> {
          /*
					 * I'm commenting these EosFiles out from this codebase becasue we do not want to automatically generate these any longer. 
					 * We don't want to generate these any longer because we are removing anything that is trademarked by EOS which includes
					 * these documents below. This should eventually be removed but I'm leaving this here for now for historical purposes and 
					 * it doesn't leave the next dev wondering why we are generating nothing. 
					 */

          //new EosFiles(){ Url = baseUrl + "EOS-LMA-Questionaire.pdf"                      , ImplementerOnly = false    , Name = "LMA Questionnaire" , SvgUrl = "https://s3.us-east-1.amazonaws.com/Radial/EosTools/svg/EOS-LMA-Questionaire.svg" },
          //new EosFiles(){ Url = baseUrl + "EOS-Tool-Followed-by-all-Checklist.pdf"        , ImplementerOnly = false    , Name = "Followed-By-All Checklist" , SvgUrl = "https://s3.us-east-1.amazonaws.com/Radial/EosTools/svg/EOS-Tool-Followed-by-all-Checklist.svg" },
          //new EosFiles(){ Url = baseUrl + "EOSPeopleAnalyzerOnePagePerformanceReview.pdf" , ImplementerOnly = false    , Name = "Performance Evaluation Review" , SvgUrl = "https://s3.us-east-1.amazonaws.com/Radial/EosTools/svg/EOSPeopleAnalyzerOnePagePerformanceReview.svg" },
          //new EosFiles(){ Url = baseUrl + "EOS_Tools_3_Step_Process.pdf"                  , ImplementerOnly = false    , Name = "3 Step Process Documenter™" , SvgUrl = "https://s3.us-east-1.amazonaws.com/Radial/EosTools/svg/EOS_Tools_3_Step_Process.svg" },
          //new EosFiles(){ Url = baseUrl + "EOS_Tools_Accountability_Chart.pdf"            , ImplementerOnly = false    , Name = "The Organizational Chart™" , SvgUrl = "https://s3.amazonaws.com/Radial/EosTools/svg/EOS_Tools_Accountability_Chart.svg" },
          //new EosFiles(){ Url = baseUrl + "EOS_Tools_Delegate_Elevate.pdf"                , ImplementerOnly = false    , Name = "Delegate and Elevate™" , SvgUrl = "https://s3.us-east-1.amazonaws.com/Radial/EosTools/svg/EOS_Tools_Delegate_Elevate.svg" },
          //new EosFiles(){ Url = baseUrl + "EOS_Tools_HR_Process.pdf"                      , ImplementerOnly = false    , Name = "The H/R Process" , SvgUrl = null },
          //new EosFiles(){ Url = baseUrl + "EOS_Tools_VTO.pdf"                             , ImplementerOnly = false    , Name = "The Vision/Traction Organizer™" , SvgUrl = "https://s3.us-east-1.amazonaws.com/Radial/EosTools/svg/EOS_Tools_VTO.svg" },
          //new EosFiles(){ Url = baseUrl + "SWOT_Analysis.pdf"                             , ImplementerOnly = false    , Name = "SWOT Analysis" , SvgUrl = "https://s3.us-east-1.amazonaws.com/Radial/EosTools/svg/SWOT%20Analysis.svg" },
        });

        return gen.Where(x => !x.ImplementerOnly || hasImplementer).Select(x => {
          var res = DocumentItemVM.CreateLink(new DocumentItemLinkSettings() {
            Url = x.Url,
            CreateTime = null,
            Description = x.Description,
            Name = x.Name,
            IconHint = "PDF",
            Generated = true,
            CanDelete = false
          }, new DocumentItemSettings(folder));
          res.OuterClass = DocumentItemVM.GetOuterCssClass(DocumentItemType.EncryptedFile);
          res.IconClass = DocumentItemVM.GetItemIconCssClass(DocumentItemType.EncryptedFile);

          if (!string.IsNullOrWhiteSpace(x.SvgUrl)) {
            res.Menu.Add(MenuItems.EditCopyViaWhitboard(res, x.SvgUrl));
          }
          return res;

        }).ToList();
      }
      return new List<DocumentItemVM>();
    }

    public async Task OnBeforeLoad(ISession s, UserOrganizationModel caller, DocumentsFolder folder) {

      //var gen = s.GetSettingOrDefault(Variable.Names.FOLDER_EOSTOOLS, () => new List<GS> {
      //			GS.AutoPopFolder("Meetings", FolderConsts.MeetingListingFolder, "L10",null,false),
      //			GS.AutoPopFolder("People Tools", FolderConsts.PeopleToolsFolder, "People", null,false)
      //		});
    }
    public async Task OnAfterLoad(DocumentsFolderVM folderVM) {
      //noop
    }

    public bool ShouldExecute(ISession s, DocumentsFolder folder) {
      return FolderConsts.EosToolsFolder.InterceptorMatches(folder);
    }


    public async Task OnAfterLink(ISession s, UserOrganizationModel caller, DocumentsFolder folder, DocumentItemLocation location) {
      //noop
    }

    public async Task OnAfterUnlink(ISession s, UserOrganizationModel caller, DocumentsFolder fromFolder, DocumentItemLocation location) {
      //noop
    }
  }
}