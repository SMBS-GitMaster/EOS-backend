using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.GraphQL.Models.Mutations;
using System;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    #region Mutations

    Task<GraphQLResponseBase> EditUserSettings(UserSettingsEditModel model);

    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Mutations

    public async Task<GraphQLResponseBase> EditUserSettings(UserSettingsEditModel model)
    {
      try
      {
        await UserSettingsAccessor.UpdateUserSettings(caller, model.HasViewedFeedbackModalOnce, model.DoNotShowFeedbackModalAgain, model.Timezone, model.DrawerView, model.TransferredBusinessPlansBannerViewCount);
        return GraphQLResponseBase.Successfully();
      }
      catch (Exception ex)
      {
        return GraphQLResponseBase.Error(ex);
      }

    }

    #endregion

  }
}