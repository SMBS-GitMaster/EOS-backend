using PuppeteerSharp;
using PuppeteerSharp.Media;
using RadialReview.Middleware.Services.HeadlessBrower;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Services.HtmlRender {
  public class PuppeteerHtmlRenderService : IHtmlRenderService {

    private IPuppeteerFactory _puppeteer;
    private IOfflineFileProvider _offlineFileProvider;

    public PuppeteerHtmlRenderService(IPuppeteerFactory puppeteer, IOfflineFileProvider offlineFileProvider) {
      _puppeteer = puppeteer;
      _offlineFileProvider = offlineFileProvider;
    }


    public IOfflineFileProvider GetOfflineFileProvider() {
      return _offlineFileProvider;
    }

    public async Task GeneratePdfFromOfflineUrl(Stream destination, string url, PdfGenerationSettings settings) {
      Stream stream = null;
      try {
        stream = await OperateOnPage(url, settings.WaitForCssSelector, null, async p => {
          var pdfOptions = GeneratePdfOptions(settings);
          return await p.PdfStreamAsync(pdfOptions);
        });
        await stream.CopyToAsync(destination);
      } finally {
        stream?.Dispose();
      }
    }

    public async Task GeneratePngFromOfflineUrl(Stream destination, string url, PngGenerationSettings settings) {
      Stream stream = null;
      try {
        var vp = new ViewPortOptions() {
          Width = settings.Width,
          Height = settings.Height,
        };

        stream = await OperateOnPage(url, settings.WaitForCssSelector, vp, async p => {
          var pngOptions = GeneratePngOptions(settings);
          return await p.ScreenshotStreamAsync(pngOptions);
        });
        stream.CopyTo(destination);
      } finally {
        stream?.Dispose();
      }
    }



    private ScreenshotOptions GeneratePngOptions(PngGenerationSettings settings) {
      return new ScreenshotOptions() {
        OmitBackground = settings.OmitBackground,
        FullPage = true,
      };
    }

    private PdfOptions GeneratePdfOptions(PdfGenerationSettings settings) {
      return new PdfOptions {
        Landscape = settings.Orientation == PdfOrientation.Landscape,
        Format = new PaperFormat(settings.WidthInches, settings.HeightInches),
        DisplayHeaderFooter = false,
        FooterTemplate = "<div></div>",
        PrintBackground = true,
        MarginOptions = new MarginOptions() {
          Top = "0.25in",
          Bottom="0.25in",
          Left="0.25in",
          Right = "0.25in"
        }
        

      };

    }

    private async Task<T> OperateOnPage<T>(string url, string waitForCssSelector, ViewPortOptions viewPort, Func<Page, Task<T>> operation) {
      try {
        Browser browser = await _puppeteer.GetBrowser();
        try {
          using (var p = await browser.NewPageAsync()) {
            p.Console += async (sender, args) => {
              try {
                Debug.WriteLine(args.Message.Type+" "+args.Message.Text +" - "+args.Message.Location.LineNumber+":"+args.Message.Location.ColumnNumber);
              } catch (Exception e) {
              }
            };

            if (viewPort != null) {
              await p.SetViewportAsync(viewPort);
            }
            await p.SetOfflineModeAsync(false);
            await p.SetRequestInterceptionAsync(true);

            var ua = await browser.GetUserAgentAsync();

            p.Request += async (sender, args) => {
              var file = await _offlineFileProvider.GetFile(new Uri(args.Request.Url));
              var body = new ResponseData() {
                BodyData = file.Content,
                ContentType = file.ContentType,
                Status = file.Status,
              };
              await args.Request.RespondAsync(body);
            };

            await p.GoToAsync(url);

            if (!string.IsNullOrWhiteSpace(waitForCssSelector)) {
              try {
                await p.WaitForSelectorAsync(waitForCssSelector, new WaitForSelectorOptions() {
                  Timeout = PuppeteerFactory.FACTORY_SETTINGS.Timeout,
                });
              } catch (Exception e) {
                var result = await p.GetContentAsync();
              }
            }


            return await operation(p);
          }
        } finally {
          await _puppeteer.PutBrowser(browser);
        }
      } catch (Exception e) {
        throw;
      }

    }
  }
}
