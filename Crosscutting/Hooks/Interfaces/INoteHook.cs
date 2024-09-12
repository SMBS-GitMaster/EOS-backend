using NHibernate;
using RadialReview.Models;
using RadialReview.Models.L10;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {

	public class INoteHookUpdates {
	//	public INoteHookUpdates(string updateSource) {
	//		UpdateSource = updateSource;
	//	}
  
	//	public string UpdateSource { get; set; }

	}

	public interface INoteHook : IHook {
		Task CreateNote(ISession s, UserOrganizationModel caller, L10Note note);
		Task UpdateNote(ISession s, UserOrganizationModel caller, L10Note note, INoteHookUpdates updates);
	}
}
