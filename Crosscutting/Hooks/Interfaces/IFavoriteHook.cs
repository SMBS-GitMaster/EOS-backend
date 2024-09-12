using NHibernate;
using RadialReview.Models;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {

	public class IFavoriteHookUpdates {

  }

  public interface IFavoriteHook : IHook {
		Task CreateFavorite(ISession s, UserOrganizationModel caller, FavoriteModel comment);
		Task UpdateFavorite(ISession s, UserOrganizationModel caller, FavoriteModel comment, IFavoriteHookUpdates updates);
  }
}
