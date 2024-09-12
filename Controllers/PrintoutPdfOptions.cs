namespace RadialReview.Core.Controllers
{
  public partial class QuarterlyController
  {
    public class PrintoutPdfOptions {
			public bool coverPage { get; set; }
			public bool issues { get; set; }
			public bool todos { get; set; }
			public bool scorecard { get; set; }
			public bool rocks { get; set; }
			public bool headlines { get; set; }
			public bool vto { get; set; }
			public bool l10 { get; set; }
			public bool acc { get; set; }
			public bool pa { get; set; }
		}
	}
}
