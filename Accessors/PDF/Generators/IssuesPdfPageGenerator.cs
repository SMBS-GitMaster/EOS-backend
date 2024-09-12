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
  public class IssuesPdfPageGenerator : IPdfPageGenerator {

    private const string _partialView = "~/Views/Quarterly/IssuesPartial.cshtml";

    private AngularRecurrence recurrence;
    private AngularOrganization organization;
    private TermsCollection terms;

    public IssuesPdfPageGenerator(AngularRecurrence recurrence, AngularOrganization organization, TermsCollection terms) {
      this.recurrence = recurrence;
      this.organization = organization;
      this.terms = terms;
    }

    public string GetPageName() {
      return terms.GetTerm(TermKey.Issues);
    }


    public async Task GeneratePdf(IHtmlRenderService renderer, Stream destination) {
      var _viewModel = GetViewModel();
      var html = await ViewUtility.RenderPartial(_partialView, _viewModel).ExecuteAsync();
      await renderer.GeneratePdfFromHtml(destination, html, new PdfGenerationSettings());

    }

    private IssuesPdfViewModel GetViewModel() {
      var issues = recurrence.IssuesList.Issues
              .OrderBy(x => x.Owner.Name)
              .ThenBy(x => x.CreateTime)
              .Select((h, i) => new IssuesPdfViewModel.IssuesPdfDto {
                Issues = h.Name,
                Owner = h.Owner.NotNull(x => x.Name),
                Number = "" + (i + 1)
              }).ToList();

      return (new IssuesPdfViewModel {
        Company = organization.Name,
        Image = organization.ImageUrl,
        Issues = issues,
        Terms = terms
      });
    }
  }
}