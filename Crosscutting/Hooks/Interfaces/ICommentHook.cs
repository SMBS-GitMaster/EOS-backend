using NHibernate;
using RadialReview.Models;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {

	public class ICommentHookUpdates {
	//	public ICommentHookUpdatess(string updateSource) {
	//		UpdateSource = updateSource;
	//	}
  
	//	public string UpdateSource { get; set; }

	}

	public interface ICommentHook : IHook {
		Task CreateComment(ISession s, UserOrganizationModel caller, CommentModel comment);
		Task UpdateComment(ISession s, UserOrganizationModel caller, CommentModel comment, ICommentHookUpdates updates);
  }
}
