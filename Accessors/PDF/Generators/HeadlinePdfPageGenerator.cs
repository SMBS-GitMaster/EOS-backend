using RadialReview.Accessors.PDF.Generators.ViewModels;
using RadialReview.Core.Models.Terms;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Organization;
using RadialReview.Utilities;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors.PDF.Generators {
  public class HeadlinePdfPageGenerator : IPdfPageGenerator {

    private const string _partialView = "~/Views/Quarterly/HeadlinesPartial.cshtml";

    private AngularRecurrence recurrence;
    private AngularOrganization organization;
    private TermsCollection terms;

    public HeadlinePdfPageGenerator(AngularRecurrence recurrence, AngularOrganization organization, TermsCollection terms) {
      this.recurrence = recurrence;
      this.organization = organization;
      this.terms = terms;
    }

    public string GetPageName() {
      return terms.GetTerm(TermKey.Headlines);
    }


    public async Task GeneratePdf(IHtmlRenderService renderer, Stream destination) {
      var _viewModel = GetViewModel();
      var html = await ViewUtility.RenderPartial(_partialView, _viewModel).ExecuteAsync();
      await renderer.GeneratePdfFromHtml(destination, html, new PdfGenerationSettings());

    }

    private HeadlinesPdfViewModel GetViewModel() {
      var headlines = recurrence.Headlines
                .OrderBy(x => x.Owner.Name)
                .ThenBy(x => x.CreateTime)
                .Select(h => new HeadlinesPdfViewModel.HeadlinesPdfDto {
                  Headline = h.Name,
                  Owner = h.Owner.NotNull(x => x.Name)
                }).ToList();

      return new HeadlinesPdfViewModel {
        Company = organization.Name,
        Headlines = headlines,
        Image = organization.ImageUrl,
        Terms = terms
      };
    }
  }
}
