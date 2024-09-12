namespace RadialReview.Accessors.PDF {
	public enum PdfPageOrientation {
		Portrait,
		Landscape
	}

	public class PdfPageSettings {

		public bool HasFooterOnFirstPage { get; set; }
		public PdfPageOrientation Orientation { get; set; }

	}
}
