using System.Collections.Generic;
using System.Linq;
using RadialReview.Models.L10.VM;

namespace RadialReview.Core.Controllers
{
  public partial class L10Controller
  {
    public class IssuesListVm {
			public string connectionId { get; set; }

			public List<IssueListItemVm> issues { get; set; }

			public string orderby { get; set; }

			public IssuesListVm() {
				issues = new List<IssueListItemVm>();
			}

			public List<long> GetAllIds() {
				return issues.SelectMany(x => x.GetAllIds()).Distinct().ToList();
			}

			public class IssueListItemVm {
				public long id { get; set; }

				public List<IssueListItemVm> children { get; set; }

				public IssueListItemVm() {
					children = new List<IssueListItemVm>();
				}

				public List<long> GetAllIds() {
					var o = new List<long>()
					{id};
					if (children != null) {
						o.AddRange(children.SelectMany(x => x.GetAllIds()));
					}

					return o;
				}
			}

			public List<IssueEdit> GetIssueEdits() {
				return issuesRecurse(null, issues).ToList();
			}

			private IEnumerable<IssueEdit> issuesRecurse(long? parentIssueId, List<IssueListItemVm> data) {
				if (data == null) {
					return new List<IssueEdit>();
				}

				var output = data.Select((x, i) => new IssueEdit() { RecurrenceIssueId = x.id, ParentRecurrenceIssueId = parentIssueId, Order = i }).ToList();
				foreach (var d in data) {
					output.AddRange(issuesRecurse(d.id, d.children));
				}

				return output;
			}
		}
	}
}
