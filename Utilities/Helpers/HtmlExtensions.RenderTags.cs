using Microsoft.AspNetCore.Html;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Encrypt;
using System;
using System.IO;
using System.Text;

namespace RadialReview.Html {

	public class RendererSettings {
#if DEBUG
		public static bool MINIFY = false;
		public static bool APPEND_VERSION = true;
#else
		public static bool MINIFY = true;
		public static bool APPEND_VERSION = true;
#endif

		private static DefaultDictionary<string, string> VersionCache = new DefaultDictionary<string, string>(path => {
			try {
				var body = File.ReadAllText(Path.Join("/app/",path));
				return Crypto.UniqueHash(body, "51f208b4-10df-4bff-b682-2796a2f56745");
			} catch (Exception e) {
				return "na";
			}
		});

		public static string GetVersion(string path) {
			return VersionCache[path ?? ""];
		}

	}


	public class Styles {

		public static IHtmlContent Render(string path) {
			return RenderFormat("<link rel=\"stylesheet\" href=\"{0}\"/>", path);
		}
		public static IHtmlContent RenderFormat(string format, params string[] paths) {
			var b = new StringBuilder();
			foreach (var p in paths) {
				var pd = p;
				if (p.Length > 0 && p[0] == '~') {
					pd = "/wwwroot" + p.Substring(1);
				}

				var file = pd + (RendererSettings.MINIFY ? ".min" : "") + ".css";

				if (RendererSettings.APPEND_VERSION) {
					file+= "?v=" + RendererSettings.GetVersion(file);
				}

				b.AppendLine(string.Format(format, file));
			}
			return new HtmlString(b.ToString());
		}
	}
	public class Scripts {

		public static IHtmlContent Render(string path) {
			return RenderFormat("<script src=\"{0}\"></script>", path);
		}
		public static IHtmlContent RenderFormat(string format, params string[] paths) {
			var b = new StringBuilder();
			foreach (var p in paths) {
				var pd = p;
				if (p.Length > 0 && p[0] == '~') {
					pd = "/wwwroot" + p.Substring(1);
				}
				var file =  pd + (RendererSettings.MINIFY ? ".min" : "") + ".js";
				
				if (RendererSettings.APPEND_VERSION) {
					file += "?v=" + RendererSettings.GetVersion(file);
				}

				b.AppendLine(string.Format(format, file));
			}
			return new HtmlString(b.ToString());
		}
	}
}
