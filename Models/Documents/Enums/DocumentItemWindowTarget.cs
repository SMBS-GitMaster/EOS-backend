using RadialReview.Models.Documents.Enums;

namespace RadialReview.Models.Documents.Enums {
	public enum DocumentItemWindowTarget {
		Default = 0,
		Self = 1,
		NewWindow = 2,
		Parent = 3,
		Top = 4,
	}
}

namespace RadialReview {
	public static class DocumentItemWindowTargetExtensions {
		public static Microsoft.AspNetCore.Html.HtmlString GetTargetAttribute(this DocumentItemWindowTarget self) {
			switch (self) {
				case DocumentItemWindowTarget.NewWindow:
					return new Microsoft.AspNetCore.Html.HtmlString("target=\"_blank\"");
				case DocumentItemWindowTarget.Parent:
					return new Microsoft.AspNetCore.Html.HtmlString("target=\"_parent\"");
				case DocumentItemWindowTarget.Top:
					return new Microsoft.AspNetCore.Html.HtmlString("target=\"_top\"");
				default:
					return new Microsoft.AspNetCore.Html.HtmlString("");
			}

		}

	}
}