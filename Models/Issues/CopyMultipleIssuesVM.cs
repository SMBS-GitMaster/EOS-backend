using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.Issues
{
	public class CopyMultipleIssuesVM
	{
		[Required]
		public long ParentIssue_RecurrenceId { get; set; }

		[Required]
		public List<long> RecurrenceIds { get; set; }
	}
}