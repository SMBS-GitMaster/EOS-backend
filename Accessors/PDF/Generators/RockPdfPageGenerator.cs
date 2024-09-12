using RadialReview.Accessors.PDF.Generators.ViewModels;
using RadialReview.Core.Models.Terms;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Organization;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors.PDF.Generators {
  public class RockPdfPageGenerator : IPdfPageGenerator {

    private const string _partialView = "~/Views/Quarterly/RocksPartial.cshtml";

    private AngularOrganization organization;
    private AngularRecurrence recurrence;
    private AngularVTO vtoModel;
    private bool printOutRockStatus;
    private TermsCollection terms;

    public RockPdfPageGenerator(AngularOrganization organization, AngularRecurrence recurrence, AngularVTO vtoModel, bool printOutRockStatus, TermsCollection terms) {
      this.organization = organization;
      this.recurrence = recurrence;
      this.vtoModel = vtoModel;
      this.printOutRockStatus = printOutRockStatus;
      this.terms = terms;
    }
    public string GetPageName() {
      return terms.GetTerm(TermKey.Goals);
    }


    public async Task GeneratePdf(IHtmlRenderService renderer, Stream destination) {

      var _viewModel = GenerageRocksPage();
      _viewModel.CompanyRockGroup = ComputeCompanyRockGroup(_viewModel.CompanyRocks, terms);
      _viewModel.IndvidualRockGroups = ComputeRockGroups(_viewModel.IndividualRocks);

      var html = await ViewUtility.RenderPartial(_partialView, _viewModel).ExecuteAsync();
      await renderer.GeneratePdfFromHtml(destination, html, new PdfGenerationSettings());

    }

    private RocksPdfViewModel GenerageRocksPage() {
      var dateFormat = organization.DateFormat;

      // this could be refactored out
      string defaultValue = "Edit here...";

      // Header
      var headers = vtoModel.QuarterlyRocks.Headers?.Select(rock => {
        var label = (rock.K ?? "").ReplaceNewLineWithBr();
        var value = (rock.V ?? "").Replace(defaultValue, "").ReplaceNewLineWithBr();
        return Tuple.Create(label, value);
      }) ?? new Tuple<string, string>[0];

      // Rocks
      var companyRocks = recurrence.Rocks.Where(x => x.VtoRock == true)
                         .Select(r => new RocksPdfViewModel.RocksPdfDto {
                           Description = r.Name,
                           Owner = r.Owner.Name,
                           Status = r.Completion
                         });

      var individualRocks = recurrence.Rocks.Select(r => new RocksPdfViewModel.RocksPdfDto {
        Description = r.Name,
        Owner = r.Owner.Name,
        Status = r.Completion
      });

      var viewData = new RocksPdfViewModel {
        FutureDate = vtoModel.QuarterlyRocks.FutureDate.NotNull(x => x.Value.ToString(dateFormat)) ?? "",
        Headers = headers.ToList(),
        CompanyRocks = companyRocks.ToList(),
        IndividualRocks = individualRocks.ToList(),
        Company = organization.Name,
        PrintOutRockStatus = printOutRockStatus,
        Image = organization.ImageUrl,
        Terms = terms
      };

      return viewData;
    }

    private static RocksPdfViewModel.RockGroupDto ComputeCompanyRockGroup(List<RocksPdfViewModel.RocksPdfDto> viewModelCompanyRocks, TermsCollection terms) {
      return new RocksPdfViewModel.RockGroupDto {
        Owner = "COMPANY " + terms.GetTerm(TermKey.Goals).ToUpper(),
        Completed = viewModelCompanyRocks.Count(rock => rock.Status.GetValueOrDefault() == RockState.Complete),
        Total = viewModelCompanyRocks.Count(),
        Rocks = viewModelCompanyRocks
      };
    }

    private static List<RocksPdfViewModel.RockGroupDto> ComputeRockGroups(List<RocksPdfViewModel.RocksPdfDto> viewModelIndividualRocks) {
      return viewModelIndividualRocks.GroupBy(ir => ir.Owner)
        .Select(ir2 => new RocksPdfViewModel.RockGroupDto {
          Owner = ir2.Key,
          Rocks = ir2.ToList(),
          Completed = ir2.Count(rock => rock.Status.GetValueOrDefault() == RockState.Complete),
          Total = ir2.Count(),
        })
        .ToList();
    }
  }
}
