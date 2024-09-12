using PuppeteerSharp;
using System;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Services.HeadlessBrower {
	public interface IPuppeteerFactory : IDisposable {

		Task<Browser> GetBrowser();
		Task PutBrowser(Browser item);
	}
}
