using RadialReview.Middleware.Services.NotesProvider;
using System.Threading.Tasks;

namespace RadialReview.Models.Interfaces
{
	public interface IIssue
	{
		Task<string> GetIssueMessage();
		Task<string> GetIssueDetails(INotesProvider notesProvider);
	}
}
