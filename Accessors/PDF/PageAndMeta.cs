using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RadialReview.Accessors.PDF {
	public class HeaderFooterSource {
		public HeaderFooterSource(string template, bool isLeft) {
			Template = template;
			IsLeft = isLeft;
		}
		public string Template { get; set; }
		public bool IsLeft { get; set; }
	}

	public class PageAndMeta {
		private PageAndMeta() { }

		public static PageAndMeta CreateFromStream(string title, Stream stream) {
			return new PageAndMeta {
				Title = title,
				Content = stream
			};
		}

		public static PageAndMeta CreateDisabledPage() {
			return new PageAndMeta {
				Title = "-disabled-",
				Disable = true
			};
		}

		public bool HasStream() {
			return Content != null;
		}

		public Stream GetStream() {
			return Content;
		}

		public string Html { get; private set; }

		public string Title { get; private set; }
		public bool Disable { get; private set; }
		private Stream Content { get; set; }
	}
}
