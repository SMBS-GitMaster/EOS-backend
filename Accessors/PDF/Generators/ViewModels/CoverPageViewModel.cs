using System.Collections.Generic;

namespace RadialReview.Accessors.PDF.Generators.ViewModels {

	public class CoverPagePdfViewModel {
		public string UpperHeading { get; set; }
		public string Image { get; set; }
		public string LowerHeading { get; set; }
		public bool UseLogo { get; set; }
		public IEnumerable<string> CoreValues { get; set; }
		public string PrimaryHeading { get; internal set; }
		public List<PageNumber> PageNumbers { get; internal set; }
	}	
}