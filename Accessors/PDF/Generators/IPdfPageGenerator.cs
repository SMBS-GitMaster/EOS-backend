using RadialReview.Middleware.Services.HtmlRender;
using System.IO;
using System.Threading.Tasks;

namespace RadialReview.Accessors.PDF.Generators {
	public interface IPdfPageGenerator {
		string GetPageName();
		Task GeneratePdf(IHtmlRenderService renderer, Stream destination);
	}
}
