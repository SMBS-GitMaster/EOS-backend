using RadialReview.Models.Enums;
using System;
using System.ComponentModel;

namespace RadialReview.Utilities.Attributes {
	[AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
	public class IconAttribute : DisplayNameAttribute {
		public BootstrapGlyphs Glyph;
		public double Version;

		public IconAttribute(BootstrapGlyphs glyph) {
			Glyph = glyph;
		}

		public Microsoft.AspNetCore.Html.HtmlString AsHtml(string title = "") {
			var clss = Glyph.ToString().Replace("_", "-").Replace("@", "");
			return new Microsoft.AspNetCore.Html.HtmlString("<span title=\"" + title + "\" class=\"icon glyphicon glyphicon-" + clss + "\"></span>");
		}
	}
}