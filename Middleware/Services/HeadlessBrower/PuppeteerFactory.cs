using PuppeteerSharp;
using RadialReview.Utilities;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace RadialReview.Middleware.Services.HeadlessBrower {
  public struct PuppeteerFactorySettings {
    public int Timeout { get; set; }
    public bool Headless { get; set; }
  }

  public class PuppeteerFactory : IPuppeteerFactory {


    private static PuppeteerFactorySettings STANDARD_SETTINGS = new PuppeteerFactorySettings() { Headless = true, Timeout = 10000 /*10 sec*/};
    private static PuppeteerFactorySettings DEBUG_SETTINGS = new PuppeteerFactorySettings() { Headless = false, Timeout = 60*60*1000 /*1 hr*/};


    public static PuppeteerFactorySettings FACTORY_SETTINGS = STANDARD_SETTINGS; // DEBUG_SETTINGS;

    public static int MAX_BROWSERS = Config.GetAppSetting("MaxPuppeteerBrowsers", "4").TryParseInt() ?? 4;

    private ILoggerFactory _loggerFactory;
    private static ConcurrentBag<Browser> _browsers = new ConcurrentBag<Browser>();

    public PuppeteerFactory(ILoggerFactory loggerFactory) {
      _loggerFactory = loggerFactory;
    }

    public async Task<Browser> GetBrowser() {
      Browser item;

      while (_browsers.TryTake(out item)) {
        if (item.IsClosed) {
          continue;
        }
        return item;
      }

      var options = new LaunchOptions {
        Headless = FACTORY_SETTINGS.Headless,//true, //should be true        
        Args = new[] {
          "--no-sandbox",
          "--font-render-hinting=medium",
          "--force-color-profile=generic-rgb"
        }
      };

      //download if required.
      var fetcher = new BrowserFetcher();
      var version = BrowserFetcher.DefaultRevision;
      if (!fetcher.LocalRevisions().Any(x => x == version) && (await fetcher.CanDownloadAsync(version))) {
        await fetcher.DownloadAsync(version);
      }

      return await Puppeteer.LaunchAsync(options, _loggerFactory);

    }


    public async Task PutBrowser(Browser item) {
      if (item == null)
        return;
      if (item.IsClosed) {
      } else {
        if (_browsers.Count < MAX_BROWSERS) {
          _browsers.Add(item);
        } else {
          try {
            item.Dispose();
          } catch (Exception) {
          }
        }
      }
    }

    public void Dispose() {
      MAX_BROWSERS = 0;
      Browser item;

      while (_browsers.TryTake(out item)) {
        try {
          item.Dispose();
        } catch (Exception e) {
        }
      }
    }
  }
}
