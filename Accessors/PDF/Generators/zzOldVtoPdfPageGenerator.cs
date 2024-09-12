using RadialReview.Core.Models.Terms;
using RadialReview.Middleware.Services.HeadlessBrower;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Models.Angular.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RadialReview.Accessors.PDF.Generators {

  /// <summary>
  /// DO NOT USE THIS ONE
  /// </summary>
  public class OldVtoPdfPageGenerator : IPdfPageGenerator {
    private string _partialView = "~/Views/ReactApp/Element.cshtml";
    private AngularVTO _vto;
    private TermsCollection terms;

    public OldVtoPdfPageGenerator(AngularVTO vto, ITimeData timeData, TermsCollection terms) {
      _vto = vto;
      AdjustVtoTimezoneOffsets(vto, timeData.TimezoneOffset);
      this.terms = terms;
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
      var fileProvider = renderer.GetOfflineFileProvider();
      var html = await ViewUtility.RenderPartial(_partialView, _vto.CompanyImg).ExecuteAsync();


      await fileProvider.SetFile(x => (x.GetLeftPart(UriPartial.Path).ToLower() == "http://localhost/vtoprintout") ? FileResponse.FromHtml(html) : null);
      await fileProvider.SetFile(x => (x.GetLeftPart(UriPartial.Path).ToLower() == "http://localhost/vto/data/" + _vto.Id) ? FileResponse.FromJson(_vto) : null);


      //await fileProvider.SetFile(x => (x.ToString().Contains("TEST") ? FileResponse.From(_vto) : null);



      ////Supply the result of the file request.
      await renderer.GeneratePdfFromOfflineUrl(destination, "http://localhost/VtoPrintout?id=" + _vto.Id, new PdfGenerationSettings() {
        WaitForCssSelector = ".RenderComplete",
        Orientation = PdfOrientation.Landscape
      });
    }

    public string GetPageName() {
      return terms.GetTerm(TermKey.BusinessPlan);
    }
  }
}
