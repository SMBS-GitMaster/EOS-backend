using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Issues;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public class IIssueHookUpdates {
		public bool MessageChanged { get; set; }
		public bool CompletionChanged { get; set; }
		public bool OwnerChanged { get; set; }
		public bool PriorityChanged { get; set; }
		public bool RankChanged { get; set; }
		public int? oldPriority { get; set; }
		public int? oldRank { get; set; }
    public int? oldStars { get; set; }
    public bool StarsChanged { get; set; }
    public bool AwaitingSolveChanged { get; set; }
    public bool AddToDepartmentPlan { get; set; }
    public bool AddToDepartmentPlanChanged { get; set; }
    public bool CompartmentChanged { get; set; }
		public long? MovedToRecurrence { get; set; }
	}

	public interface IIssueHook : IHook {

		Task CreateIssue(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence issue);
		Task UpdateIssue(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence issue, IIssueHookUpdates updates);

    Task SendIssueTo(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence sourceIssue, IssueModel.IssueModel_Recurrence destIssue);

  }
}
