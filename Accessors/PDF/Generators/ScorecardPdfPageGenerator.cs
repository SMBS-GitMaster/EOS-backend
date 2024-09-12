using RadialReview.Accessors.PDF.Generators.ViewModels;
using RadialReview.Core.Models.Terms;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Organization;
using RadialReview.Utilities;
using System.IO;
using System.Threading.Tasks;

namespace RadialReview.Accessors.PDF.Generators {
  public class ScorecardPdfPageGenerator : IPdfPageGenerator {

    private const string _partialView = "~/Views/Quarterly/ScorecardsPartial.cshtml";

    private AngularRecurrence recurrence;
    private AngularOrganization organization;
    private TermsCollection terms;

    public ScorecardPdfPageGenerator(AngularRecurrence recurrence, AngularOrganization organization, TermsCollection terms) {
      this.recurrence = recurrence;
      this.organization = organization;
      this.terms = terms;
    }

    public string GetPageName() {
      return terms.GetTerm(TermKey.Metrics);
    }


    public async Task GeneratePdf(IHtmlRenderService renderer, Stream destination) {
      var _viewModel = GetViewModel();
      var html = await ViewUtility.RenderPartial(_partialView, _viewModel).ExecuteAsync();
      await renderer.GeneratePdfFromHtml(destination, html, new PdfGenerationSettings());
    }

    private ScorecardsPdfViewModel GetViewModel() {
      return new ScorecardsPdfViewModel {
        Terms = terms,
        Company = organization.Name,
        Scorecards = new ScorecardsPdfViewModel.ScorecardsPdfDto {

          Scorecard = recurrence.Scorecard
        },
        Image = organization.ImageUrl,
        DateFormat = organization.DateFormat,
      };
    }
  }
}