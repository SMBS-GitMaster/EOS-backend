using RadialReview.Accessors.PDF.Generators.ViewModels;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RadialReview.Accessors.PDF.Generators {
	public class CoverPagePdfPageGenerator : IPdfPageGenerator {

		private const string _partialView = "~/Views/Quarterly/CoverPagePartial.cshtml";

		private string primaryHeading;
		private string upperHeading;
		private string lowerHeading;
		private string imageUrl;
		private bool useLogo;
		private List<string> valuesList;
		private List<PageNumber> pageNumbers;

		public CoverPagePdfPageGenerator(string primaryHeading, string upperHeading, string lowerHeading, string imageUrl, bool useLogo, List<string> valuesList) {
			this.primaryHeading = primaryHeading;
			this.upperHeading = upperHeading;
			this.lowerHeading = lowerHeading;
			this.imageUrl = imageUrl;
			this.useLogo = useLogo;
			this.valuesList = valuesList;
			this.pageNumbers = new List<PageNumber>();
		}

		public string GetPageName() {
			return "Cover Page";
		}


		public async Task GeneratePdf(IHtmlRenderService renderer, Stream destination) {
			var _viewModel = GetViewModel();
			var html = await ViewUtility.RenderPartial(_partialView, _viewModel).ExecuteAsync();
			await renderer.GeneratePdfFromHtml(destination, html, new PdfGenerationSettings() {
				Orientation = PdfOrientation.Landscape
			});
		}

		public void SetPageNumbers(List<PageNumber> pageNumbers) {
			this.pageNumbers = pageNumbers;
		}

		private CoverPagePdfViewModel GetViewModel() {
			return new CoverPagePdfViewModel {
				UpperHeading = upperHeading,
				LowerHeading = lowerHeading,
				PrimaryHeading = primaryHeading,
				Image = imageUrl,
				CoreValues = valuesList,
				PageNumbers = pageNumbers,
				UseLogo = useLogo
			};
		}
	}
}