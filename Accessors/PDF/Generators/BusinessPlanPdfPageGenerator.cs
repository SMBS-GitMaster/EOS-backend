using RadialReview.Core.Controllers.BusinessPlan;
using RadialReview.Core.Models.Terms;
using RadialReview.Middleware.Services.HeadlessBrower;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Models;
using RadialReview.Models.Angular.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RadialReview.Accessors.PDF.Generators {
  public class BusinessPlanPdfPageGenerator : IPdfPageGenerator {
    private string _partialView = "~/Views/VTO/Edit.cshtml";
    private AngularVTO _vto;
    private TermsCollection terms;
    private OrganizationModel org;

    public BusinessPlanPdfPageGenerator(AngularVTO vto,OrganizationModel org, ITimeData timeData, TermsCollection terms) {
      _vto = vto;
      AdjustVtoTimezoneOffsets(vto, timeData.TimezoneOffset);
      this.terms = terms;
      this.org=org;
    }

    private static void AdjustVtoTimezoneOffsets(AngularVTO vtoModel, int offset) {
      if (vtoModel.ThreeYearPicture.FutureDate.HasValue && vtoModel.ThreeYearPicture.FutureDate.Value != vtoModel.ThreeYearPicture.FutureDate.Value.Date)
        vtoModel.ThreeYearPicture.FutureDate = vtoModel.ThreeYearPicture.FutureDate.Value.AddMinutes(offset).Date;
      if (vtoModel.OneYearPlan.FutureDate.HasValue && vtoModel.OneYearPlan.FutureDate.Value != vtoModel.OneYearPlan.FutureDate.Value.Date)
        vtoModel.OneYearPlan.FutureDate = vtoModel.OneYearPlan.FutureDate.Value.AddMinutes(offset).Date;
      if (vtoModel.QuarterlyRocks.FutureDate.HasValue && vtoModel.QuarterlyRocks.FutureDate.Value != vtoModel.QuarterlyRocks.FutureDate.Value.Date)
        vtoModel.QuarterlyRocks.FutureDate = vtoModel.QuarterlyRocks.FutureDate.Value.AddMinutes(offset).Date;
    }

    public async Task GeneratePdf(IHtmlRenderService renderer, Stream destination) {

      var model = new VTOViewModel() {
        IsPartial = true,
        Id = _vto.Id,
        VisionId =_vto._VisionId,
        //OnlyCompanyWideRocks
      };

      var settings = new SettingsViewModel();
      settings.localization = new SettingsViewModel.Localization()
      {
        lang = terms.LanguageCode,
        terms = terms.Terms,
      };

      var fileProvider = renderer.GetOfflineFileProvider();
      var viewRenderer= ViewUtility.RenderPartial(_partialView, model);
      viewRenderer.SetViewBag("Organization", org);
      viewRenderer.SetViewBag("HasBaseController", true);
      viewRenderer.SetViewBag("IsPrintout", true);
      viewRenderer.SetViewBag("Settings", settings);
      var html = await viewRenderer.ExecuteAsync();



      await fileProvider.SetFile(x => (x.GetLeftPart(UriPartial.Path).ToLower() == "http://localhost/vtoprintout") ? FileResponse.FromHtml(html) : null);
      await fileProvider.SetFile(x => (x.GetLeftPart(UriPartial.Path).ToLower() == "http://localhost/vto/data/" + _vto.Id) ? FileResponse.FromJson(_vto) : null);


      //await fileProvider.SetFile(x => (x.ToString().Contains("TEST") ? FileResponse.From(_vto) : null);



      ////Supply the result of the file request.
      await renderer.GeneratePdfFromOfflineUrl(destination, "http://localhost/VtoPrintout?id=" + _vto.Id, new PdfGenerationSettings() {
        WaitForCssSelector = ".RenderComplete.PrinoutPrepared",
        Orientation = PdfOrientation.Portrait,
        
      });
    }

    public string GetPageName() {
      return terms.GetTerm(TermKey.BusinessPlan);
    }
  }
}
