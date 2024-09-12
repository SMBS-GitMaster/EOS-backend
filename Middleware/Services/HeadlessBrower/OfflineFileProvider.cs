using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Services.HeadlessBrower {
	public class OfflineFileProvider : IOfflineFileProvider {

		private List<Func<Uri, bool>> AllowedDownloads = new List<Func<Uri, bool>>() {
			x=>x.GetLeftPart(UriPartial.Authority) == "https://s3.amazonaws.com",
			x=>x.GetLeftPart(UriPartial.Authority) == "https://contattafiles.s3.us-west-1.amazonaws.com",
			x=>x.GetLeftPart(UriPartial.Authority) == "https://fonts.googleapis.com",
			x=>x.GetLeftPart(UriPartial.Authority) == "https://fonts.gstatic.com",
			x=>x.GetLeftPart(UriPartial.Path).StartsWith("https://cdnjs.cloudflare.com/ajax/libs/react/"),
			x=>x.GetLeftPart(UriPartial.Path).StartsWith("https://cdnjs.cloudflare.com/ajax/libs/react-dom/"),
			x=>x.GetLeftPart(UriPartial.Path).StartsWith("https://cdnjs.cloudflare.com/ajax/libs/remarkable/"),
			x=>x.GetLeftPart(UriPartial.Path).StartsWith("https://cdnjs.cloudflare.com/ajax/libs/font-awesome/")			
		};

		private IWebHostEnvironment _hostingEnv;
		private IHttpClientFactory _httpClientFactory;
		private Uri BaseUri = new Uri("http://localhost/");
		public ConcurrentDictionary<string, FileResponse> Cache = new ConcurrentDictionary<string, FileResponse>();
		public List<Func<Uri, FileResponse?>> _fileInterceptors = new List<Func<Uri, FileResponse?>>();

		public OfflineFileProvider(IWebHostEnvironment hostingEnv, IHttpClientFactory httpClientFactory) {
			_hostingEnv = hostingEnv;
			_httpClientFactory = httpClientFactory;


		}

		public async Task<FileResponse> GetFile(Uri uri) {
			var defaultContentType = @"text/plain; charset=UTF-8";
			Debug.WriteLine("Getting "+uri);

			foreach (var interceptedFile in _fileInterceptors.Where(x => x != null).Select(x => x(uri))) {
				if (interceptedFile != null)
					return interceptedFile.Value;
			}

			if (uri.Scheme == "data") {
				return new FileResponse(defaultContentType, uri.ToString(), HttpStatusCode.OK);
			}


			if (AllowedDownloads.Any(f => f(uri))) {
				try {
					var url = uri.ToString();
					FileResponse output;
					if (Cache.TryGetValue(url, out output)) {
						return output;
					}

					using (var client = _httpClientFactory.CreateClient()) {
						client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.0 Safari/537.36");
						var response = await client.GetAsync(url);
						var mime = response.Content.Headers.GetValues("content-type").FirstOrDefault();
						var bytes = await response.Content.ReadAsByteArrayAsync();
						var res = new FileResponse(mime, bytes, HttpStatusCode.OK);
						Cache.TryAdd(url, res);
						return res;
					}
				} catch (Exception e) {
					return new FileResponse(defaultContentType, "", HttpStatusCode.BadRequest);
				}
			}


			if (uri.GetLeftPart(UriPartial.Authority) == BaseUri.GetLeftPart(UriPartial.Authority) && BaseUri != uri) {
				try {
					var relativePath = BaseUri.MakeRelativeUri(uri);
					if (relativePath.ToString().Contains("..")) {
						throw new DirectoryNotFoundException();
					}
					try {
						/*============= Signalr ==============*/
						if (uri.AbsolutePath.Contains("/signalr/")) {
							throw new NotImplementedException("we shouldnt get here right?");
						}
					} catch (Exception e) {
						//noop
					}
					try {
						/*============ Exact File ============*/
						var contentType = defaultContentType;
						if (uri.LocalPath.EndsWith(".css")) {
							contentType = "text/css";
						} else if (uri.LocalPath.EndsWith(".js")) {
							contentType = "application/javascript";
						}
						IFileInfo fileInfo = null;

						try {
							if (uri.LocalPath.Contains(".."))
								throw new DirectoryNotFoundException();
							if (uri.LocalPath.ToLower().StartsWith("/wwwroot/")) {
								fileInfo = _hostingEnv.WebRootFileProvider.GetFileInfo(uri.LocalPath.Substring("/wwwroot/".Length));
							} else if (uri.LocalPath.ToLower().StartsWith("/scripts/") || uri.LocalPath.ToLower().StartsWith("/content/")) {
								fileInfo = _hostingEnv.ContentRootFileProvider.GetFileInfo(uri.LocalPath);
							}
						} catch (Exception e) {
							throw;
						}
						if (fileInfo != null && fileInfo.Exists) {
							using (var file = fileInfo.CreateReadStream()) {
								using (var ms = new MemoryStream()) {
									await file.CopyToAsync(ms);
									return new FileResponse(contentType, ms.ToArray(), HttpStatusCode.OK);
								}
							}
						}
					} catch (Exception e) {
				}
				} catch (Exception e) {
					return new FileResponse(defaultContentType, "Error with file: " + (uri.ToString()), HttpStatusCode.BadRequest);
				}
			}
			Debug.WriteLine("Failed to load: " + uri);
			return new FileResponse(defaultContentType, "", HttpStatusCode.BadRequest);
		}

		public async Task SetFile(Func<Uri, FileResponse?> provider) {
			_fileInterceptors.Add(provider);
		}
	}
}
