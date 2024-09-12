using RadialReview.Areas.People.Accessors.PDF;
using RadialReview.Areas.People.Angular;
using RadialReview.Core.Models.Terms;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Utilities.DataTypes;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RadialReview.Accessors.PDF.Generators {
  public class PeopleAnalyzerPdfPageGenerator : IPdfPageGenerator {

    private ITimeData _timeData;
    private AngularPeopleAnalyzer _peopleAnalyzer;
    private TermsCollection terms;

    public PeopleAnalyzerPdfPageGenerator(ITimeData timeData, AngularPeopleAnalyzer peopleAnalyzer, TermsCollection terms) {
      _timeData = timeData;
      _peopleAnalyzer = peopleAnalyzer;
      this.terms = terms;
    }

    public async Task GeneratePdf(IHtmlRenderService renderer, Stream destination) {
      var doc = PdfAccessor.CreateDoc(null, terms.GetTerm(TermKey.RightPersonRightSeat));
      var pdfSettings = new PdfSettings() { };
      var r = PeopleAnalyzerPdf.AppendPeopleAnalyzer(_timeData, terms, doc, _peopleAnalyzer, pdfSettings, DateTime.MaxValue);
      r.Save(destination, false);
    }

    public string GetPageName() {
      return terms.GetTerm(TermKey.RightPersonRightSeat);
    }
  }
}
