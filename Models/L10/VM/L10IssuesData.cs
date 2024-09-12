using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Models.Issues;

namespace RadialReview.Models.L10.VM
{
  public class IssuesData {
		public long recurrence_issue { get; set; }
		public long issue { get; set; }
		public long createtime { get; set; }
		public IssuesData[] children { get; set; }
		public String message { get; set; }
		public String details { get; set; }
		public bool @checked { get; set; }
		public String owner { get; set; }
		public long? accountable { get; set; }
		public String imageUrl { get; set; }
		public long? createdDuringMeetingId { get; set; }
		public int priority { get; set; }

		public string FromWhere { get; set; }
		public string _MovedToMeetingName { get; set; }

		public int rank { get; set; }
		public bool awaitingsolve { get; set; }
		public bool markedforclose { get; set; }

		public static IssuesData FromIssueRecurrence(IssueModel.IssueModel_Recurrence issueRecur) {
			var issue = new IssuesData() {
				priority = issueRecur.Priority,
				@checked = issueRecur.CloseTime != null,
				createtime = issueRecur.CreateTime.NotNull(x => x.ToJavascriptMilliseconds()),
				details = issueRecur.Issue.Description,
				message = issueRecur.Issue.Message,
				recurrence_issue = issueRecur.Id,
				issue = issueRecur.Issue.Id,
				owner = issueRecur.Owner.NotNull(x => x.GetName()),
				imageUrl = issueRecur.Owner.NotNull(x => x.ImageUrl(true, ImageSize._64)) ?? "/i/placeholder",
				createdDuringMeetingId = issueRecur.Issue.CreatedDuringMeetingId,
				rank = issueRecur.Rank,
				awaitingsolve = issueRecur.AwaitingSolve,
				markedforclose = issueRecur.MarkedForClose,
				FromWhere = issueRecur.FromWhere,
				_MovedToMeetingName = issueRecur._MovedToMeetingName,
			};
			if (issueRecur.Owner != null) {
				issue.accountable = issueRecur.Owner.Id;
			}
			return issue;
		}
	}
}
