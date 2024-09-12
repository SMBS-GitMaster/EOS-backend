using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.Issues {
	public class HeadlineIssueVM : IssueVM {
		[Required]
		public long HeadlineId { get; set; }
	}
}
