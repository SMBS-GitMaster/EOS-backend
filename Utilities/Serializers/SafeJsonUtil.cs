using Newtonsoft.Json;

namespace RadialReview.Utilities.Serializers {
	public static class SafeJsonUtil {

		public static Microsoft.AspNetCore.Html.HtmlString SafeJsonSerialize(this object o) {
			return new Microsoft.AspNetCore.Html.HtmlString(SafeJsonSerializeString(o));
		}
		public static string SafeJsonSerializeString(this object o) {
			var settings = new JsonSerializerSettings();
			settings.StringEscapeHandling = StringEscapeHandling.EscapeHtml;
			return JsonConvert.SerializeObject(o, settings);
		}
	}
}