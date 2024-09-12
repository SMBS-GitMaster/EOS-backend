using System;
using System.Linq;
using Microsoft.AspNetCore.Html;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RadialReview {
	public class ExtraTab {
		public class Tab {
			public HtmlString Text { get; set; }
			public string Url { get; set; }
			public string Page { get; set; }
			public HtmlString Html { get; set; }
		}

		public static List<Tab> Create(params String[] textUrl) {
			var output = new List<Tab>();
			for (int i = 0; i < textUrl.Count(); i += 2) {
				var n = new HtmlString(textUrl[i]);
				var nn = Regex.Replace(textUrl[i], @"<[^>]+>|", "").Trim();
				output.Add(new Tab() { Page = nn, Text = n, Url = textUrl[i + 1] });
			}
			return output;
		}
	}
}