using RadialReview.Middleware.Services.NotesProvider;
using System.Threading.Tasks;

namespace RadialReview.Models.Interfaces
{
	public interface ITodo
	{
		Task<string> GetTodoMessage();
		Task<string> GetTodoDetails(INotesProvider notesProvider);
	}
}
