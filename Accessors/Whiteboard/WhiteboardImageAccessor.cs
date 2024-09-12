using RadialReview.Middleware.Services.HeadlessBrower;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RadialReview.Accessors.Whiteboard {
	public class WhiteboardImageAccessor {
		public static async Task GenerateImageAsync(UserOrganizationModel caller, Stream destination, IHtmlRenderService htmlRenderer, string whiteboardId) {

			var wb = WhiteboardAccessor.GetWhiteboard(caller, whiteboardId, true);
			var view = ViewUtility.RenderView("~/views/whiteboard/index.cshtml", wb);
			view.ViewData["HasBaseController"] = true;
			var viewStr = await view.ExecuteAsync();


			viewStr += $@"<script>
(function(){{
setTimeout(function(){{console.log('injected diffs'); applyDiffs();}},400);
let printInterval = setInterval(function(){{
	if ($('body.RenderComplete').length!=0){{		
		clearInterval(printInterval);
		prepareScreenshot();
	}}
}},50);
}})()</script>";

			var fakeUrl = "http://localhost/" + Guid.NewGuid();

			var offlineProvider = htmlRenderer.GetOfflineFileProvider();
			await offlineProvider.SetFile(x => x.ToString() == fakeUrl ? FileResponse.FromHtml(viewStr) : null);
			await htmlRenderer.GeneratePngFromOfflineUrl(destination, fakeUrl, new PngGenerationSettings() {
				Width = 1600,
				Height = 1200,
				WaitForCssSelector = ".ScreenshotReady"
			});
		}
	}
}
