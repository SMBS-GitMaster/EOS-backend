using NHibernate;
using RadialReview.Accessors;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries

    #endregion

    #region Mutations
    Task SetV3BusinessPlanId(ISession s, long? v3BusinessPlanId);
    #endregion

  }


  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    #endregion

    #region Mutations
    public async Task SetV3BusinessPlanId(ISession s, long? bizPlanId)
    {
      await OrganizationAccessor.SetV3BusinessPlanId(s, caller, bizPlanId);    
    }
    #endregion

  }
}