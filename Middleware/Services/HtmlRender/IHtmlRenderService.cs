using RadialReview.Middleware.Services.HeadlessBrower;
using System.IO;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Services.HtmlRender {
	public enum PdfOrientation {
		Portrait,
		Landscape
	}

	public class PdfGenerationSettings {
		public PdfGenerationSettings(PdfOrientation orientation = PdfOrientation.Portrait, decimal widthInches = 8.5m, decimal heightInches = 11m) {
			Orientation = orientation;
			WidthInches = widthInches;
			HeightInches = heightInches;
		}

		public PdfOrientation Orientation { get; set; }
		public decimal WidthInches { get; set; }
		public decimal HeightInches { get; set; }
		public string WaitForCssSelector { get; set; }
	}
	public class PngGenerationSettings {
		public PngGenerationSettings() {
			Width = 800;
			Height = 600;

		}
		public int Width { get; set; }
		public int Height { get; set; }


		public bool OmitBackground { get; set; }
		public string WaitForCssSelector { get; set; }
	}

	public interface IHtmlRenderService {
		IOfflineFileProvider GetOfflineFileProvider();
		Task GeneratePngFromOfflineUrl(Stream destination, string url, PngGenerationSettings settings);
		Task GeneratePdfFromOfflineUrl(Stream destination, string url, PdfGenerationSettings settings);
	}
}
