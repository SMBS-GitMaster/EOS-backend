using RadialReview.Middleware.Services.HeadlessBrower;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Services.HtmlRender {
	public static class HtmlRendererExtensions {

		public static async Task GeneratePdfFromHtml(this IHtmlRenderService renderer, Stream destination, string html, PdfGenerationSettings settings) {
			var fake_url = "http://localhost/" + Guid.NewGuid();
			var offlineProvider = renderer.GetOfflineFileProvider();
			await offlineProvider.SetFile(x => x.ToString() == fake_url ? FileResponse.FromHtml(html) : null);
			await renderer.GeneratePdfFromOfflineUrl(destination, fake_url, settings);

		}

	}
}
