using RadialReview.Core.Models.Terms;
using RadialReview.Engines;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Utilities.DataTypes;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RadialReview.Accessors.PDF.Generators {
  public class AccountabilityChartPdfPageGenerator : IPdfPageGenerator {

    private Chart<AngularAccountabilityChart> chart;
    private List<AccountabilityPrintoutSettings> settings;
    private long orgId;
    private TermsCollection terms;

    public AccountabilityChartPdfPageGenerator(Chart<AngularAccountabilityChart> chart, List<AccountabilityPrintoutSettings> settings, long orgId, TermsCollection terms) {
      this.chart = chart;
      this.settings = settings;
      this.orgId = orgId;
      this.terms = terms;
    }

    public string GetPageName() {
      return terms.GetTerm(TermKey.OrganizationalChart);
    }


    public async Task GeneratePdf(IHtmlRenderService renderer, Stream destination) {
      chart.data.ExpandAll();
      using (var pages = new StreamAndMetaCollection()) {
        await AccountabilityChartPDF.RenderPages(pages, terms, renderer, chart, orgId, settings, null);
        await PdfEngine.Merge(pages, destination);
      }
    }
  }
}
