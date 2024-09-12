using RadialReview.Accessors.PDF.Generators.ViewModels;
using RadialReview.Core.Models.Terms;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Organization;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors.PDF.Generators {
  public class TodoPdfPageGenerator : IPdfPageGenerator {

    private const string _partialView = "~/Views/Quarterly/TodosPartial.cshtml";

    private AngularRecurrence recurrence;
    private AngularOrganization organization;
    private TermsCollection terms;

    public TodoPdfPageGenerator(AngularRecurrence recurrence, AngularOrganization organization, TermsCollection terms) {
      this.recurrence = recurrence;
      this.organization = organization;
      this.terms = terms;
    }

    public string GetPageName() {
      return terms.GetTerm(TermKey.ToDos);
    }

    public async Task GeneratePdf(IHtmlRenderService renderer, Stream destination) {
      var _viewModel = GetViewModel();
      var html = await ViewUtility.RenderPartial(_partialView, _viewModel).ExecuteAsync();
      await renderer.GeneratePdfFromHtml(destination, html, new PdfGenerationSettings());
    }


    private TodosPdfViewModel GetViewModel() {
      var dateFormat = organization.DateFormat;
      var offset = organization.Timezone.NotNull(x => x.Offset);

      var todos = recurrence.Todos
              .OrderBy(x => x.Owner.Name)
              .ThenBy(x => x.CreateTime)
              .Select((h, i) => new TodosPdfViewModel.TodosPdfDto {
                Number = "" + (i + 1),
                Todo = h.Name,
                Owner = h.Owner.NotNull(x => x.Name),
                DueDate = h.DueDate.NotNull(x => TimeData.ConvertFromServerTime(x.Value, offset).ToString(dateFormat))
              }).ToList();

      return new TodosPdfViewModel {
        Company = organization.Name,
        Todos = todos,
        Image = organization.ImageUrl,
        Terms = terms
      };
    }

  }
}
