using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.Issues {
	public class RockIssueVM : IssueVM {
		[Required]
		public long RockId { get; set; }
	}
}
