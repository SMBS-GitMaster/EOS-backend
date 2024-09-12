using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace RadialReview.Html {
	public static class ViewDataDictionaryExtensions {
		public static ViewDataDictionary AddOrUpdate(this ViewDataDictionary vd, string key, object value) {
			vd[key] = value;
			return vd;
		}
	}
}
