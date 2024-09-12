using System.Threading.Tasks;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.Interfaces;

namespace RadialReview {
	public static class IssuesExtensions {
		public async static Task<string> IssueMessage(this IIssue self) {
			if (self == null)
				return "Not entered.";
			return await self.GetIssueMessage();

		}

		public async static Task<string> IssueDetails(this IIssue self, INotesProvider notesProvider) {
			if (self == null)
				return "Not entered.";
			return await self.GetIssueDetails(notesProvider);
		}
	}
}