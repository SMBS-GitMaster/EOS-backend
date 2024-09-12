using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.Issues {
	public class ScoreCardIssueVM : IssueVM {
		[Required]
		public long MeasurableId { get; set; }

	}
}
