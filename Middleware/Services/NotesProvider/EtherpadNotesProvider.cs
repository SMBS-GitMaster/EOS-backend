using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RadialReview.Exceptions;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Services.NotesProvider {
	public class EtherpadNotesProvider : INotesProvider {

		private IHttpClientFactory _httpClientFactory;
		private ILogger<EtherpadNotesProvider> _logger;

		public EtherpadNotesProvider(IHttpClientFactory httpFactory, ILogger<EtherpadNotesProvider> logger) {
			_httpClientFactory = httpFactory;
			_logger = logger;
		}

		public async Task CreatePad(string padid, string text) {
			try {
				using (var client = _httpClientFactory.CreateClient()) {
					{
						var baseUrl = Config.NotesUrl("api/1/createPad?apikey=" + Config.NoteApiKey() + "&padID=" + padid);
						HttpResponseMessage response = await client.GetAsync(baseUrl);
						HttpContent responseContent = response.Content;
						using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
							var result = await reader.ReadToEndAsync();
							int code = JsonConvert.DeserializeObject<dynamic>(result).code;
							string message = JsonConvert.DeserializeObject<dynamic>(result).message;
							if (code != 0 && message != "padID does already exist") {
								throw new PermissionsException("Error " + code + ": " + message);
							}
						}
					}

					if (!string.IsNullOrWhiteSpace(text)) {
						var chunkSize = 100;
						var subtexts = WholeChunks(text, chunkSize);

						foreach (var t in subtexts) {
							var urlText = "&text=" + WebUtility.UrlEncode(t);
							var baseUrl = Config.NotesUrl("api/1.2.13/appendText?apikey=" + Config.NoteApiKey() + "&padID=" + padid + urlText);
							HttpResponseMessage response = await client.GetAsync(baseUrl);
							HttpContent responseContent = response.Content;
							using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
								var result = await reader.ReadToEndAsync();
								int code = JsonConvert.DeserializeObject<dynamic>(result).code;
								string message = JsonConvert.DeserializeObject<dynamic>(result).message;
								if (code != 0) {
									throw new PermissionsException("Error " + code + ": " + message);
								}
							}
						}
					}
				}

			} catch (Exception e) {
				_logger.LogError("Error EtherpadNotesProvider.CreatePad", e);
			}
		}

		public async Task<HtmlString> GetHtmlForPad(string padid) {
			try {
				using (var client = _httpClientFactory.CreateClient()) {
					var baseUrl = Config.NotesUrl("api/1/getHTML?apikey=" + Config.NoteApiKey() + "&padID=" + padid);
					HttpResponseMessage response = await client.GetAsync(baseUrl);
					HttpContent responseContent = response.Content;
					using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
						var result = await reader.ReadToEndAsync();
						int code = JsonConvert.DeserializeObject<dynamic>(result).code;
						string message = JsonConvert.DeserializeObject<dynamic>(result).message;
						if (code != 0) {
							if (message == "padID does not exist") {
								return new HtmlString("");
							}
							throw new PermissionsException("Error " + code + ": " + message);
						}

						var html = (string)(JsonConvert.DeserializeObject<dynamic>(result).data.html);
						html = html.Substring("<!DOCTYPE HTML><html><body>".Length, html.Length - ("</body></html>".Length + "<!DOCTYPE HTML><html><body>".Length));
						return new HtmlString(html);
					}
				}
			} catch (Exception e) {
				_logger.LogError("Error EtherpadNotesProvider.GetHtml", e);
				return new HtmlString("");
			}
		}

		public async Task<string> GetReadonlyUrl(string padid) {
			try {
				using (var client = _httpClientFactory.CreateClient()) {
					var baseUrl = Config.NotesUrl("api/1/getReadOnlyID?apikey=" + Config.NoteApiKey() + "&padID=" + padid);
					HttpResponseMessage response = await client.GetAsync(baseUrl);
					HttpContent responseContent = response.Content;
					using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
						var result = await reader.ReadToEndAsync();
						int code = JsonConvert.DeserializeObject<dynamic>(result).code;
						string message = JsonConvert.DeserializeObject<dynamic>(result).message;
						if (code != 0) {
							if (message == "padID does not exist") {
								await CreatePad(padid, null);
								return await GetReadonlyUrl(padid);
							}

							throw new PermissionsException("Error " + code + ": " + message);
						}
						return (string)(JsonConvert.DeserializeObject<dynamic>(result).data.readOnlyID);
					}
				}
			} catch (Exception e) {
				_logger.LogError("Error EtherpadNotesProvider.GetReadOnlyID", e);
				return "r.0a198a5362822f17b4690e5e66a6fba3"; 
			}
		}

		public async Task<string> GetTextForPad(string padid) {
			try {
				if (string.IsNullOrWhiteSpace(padid)) {
					return "";
				}

				using (var client = _httpClientFactory.CreateClient()) {
					var baseUrl = Config.NotesUrl("api/1/getText?apikey=" + Config.NoteApiKey() + "&padID=" + padid);
					HttpResponseMessage response = await client.GetAsync(baseUrl);
					HttpContent responseContent = response.Content;
					using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
						var result = await reader.ReadToEndAsync();
						int code = JsonConvert.DeserializeObject<dynamic>(result).code;
						string message = JsonConvert.DeserializeObject<dynamic>(result).message;
						if (code != 0) {
							if (message == "padID does not exist") {
								return "";
							}
							throw new PermissionsException("Error " + code + ": " + message);
						}
						return (string)(JsonConvert.DeserializeObject<dynamic>(result).data.text);
					}
				}
			} catch (Exception e) {
				_logger.LogError("Error EtherpadNotesProvider.GetText", e);
				return "";
			}
		}


		private static IEnumerable<string> WholeChunks(string str, int chunkSize) {
			for (int i = 0; i < str.Length; i += chunkSize) {
				if (str.Length - i >= chunkSize) {
					yield return str.Substring(i, chunkSize);
				} else {
					yield return str.Substring(i);
				}
			}
		}

	}
}
