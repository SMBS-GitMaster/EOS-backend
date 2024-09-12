using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.GraphQL.Models;
using RadialReview.Models;

namespace RadialReview.Core.Repositories
{
  public static class UserSettingsTransformer
  {

    #region Public Methods

    public static UserSettingsQueryModel TransformTimeZone(UserOrganizationModel caller, UserSettings model)
    {
      if(model.WorkspaceHomeId == -1)
      {
        model.WorkspaceHomeId = caller.PrimaryWorkspace.WorkspaceId;
        model.WorkspaceHomeType = RadialReview.Models.Enums.DashboardType.Standard;
      }

      return new UserSettingsQueryModel
      {
        CreateTime = model.CreateTime,
        DeleteTime = model.DeleteTime,
        DoNotShowFeedbackModalAgain = model.DoNotShowFeedbackModalAgain,
        HasViewedFeedbackModalOnce = model.HasViewedFeedbackModalOnce,
        DrawerView = ((gqlDrawerView)model.DrawerView).ToString(),
        TransferredBusinessPlansBannerViewCount = model.TransferredBusinessPlansBannerViewCount,
        Id = model.Id,
        Timezone = model.Timezone,
        UserId = model.UserId,
        WorkspaceHomeId = model.WorkspaceHomeId,
        WorkspaceHomeType = model.WorkspaceHomeType?.ToString(),
      };
    }

    #endregion

  }
}
