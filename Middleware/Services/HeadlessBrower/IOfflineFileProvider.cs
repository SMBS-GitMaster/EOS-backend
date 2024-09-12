using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Services.HeadlessBrower {

	public struct FileResponse {
		public string ContentType { get; set; }
		public byte[] Content { get; set; }
		public HttpStatusCode Status { get; set; }

		public FileResponse(string contentType, string content, HttpStatusCode status = HttpStatusCode.OK) {
			ContentType = contentType;
			Content = Encoding.UTF8.GetBytes(content);
			Status = status;
		}
		public FileResponse(string contentType, byte[] content, HttpStatusCode status = HttpStatusCode.OK) {
			ContentType = contentType;
			Content = content;
			Status = status;
		}

		public static FileResponse FromJson<T>(T obj) {
			return new FileResponse("application/json", JsonConvert.SerializeObject(obj, Formatting.Indented));
		}
		public static FileResponse FromText(string text) {
			return new FileResponse(@"text/plain; charset=UTF-8", text);
		}

		public static FileResponse FromHtml(string html) {
			return new FileResponse(@"text/html; charset=UTF-8", html);

		}
	}


	public interface IOfflineFileProvider {

		Task<FileResponse> GetFile(Uri uri);
		Task SetFile(Func<Uri, FileResponse?> provider);

	}
}
