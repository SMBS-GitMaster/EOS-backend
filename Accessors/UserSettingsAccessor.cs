using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors
{
  public class UserSettingsAccessor
  {

    #region Public Methods

    public static async Task<UserSettings> UpdateUserSettings(UserOrganizationModel caller, bool? hasViewedFeedbackModalOnce, bool? doNotShowFeedbackModalAgain,
                                                                                            string timezone, string drawerView, int? transferredBusinessPlansBannerViewCount)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        var transaction = s.BeginTransaction();
        var settings = s.Query<UserSettings>().Where(_ => _.UserId == caller.Id).FirstOrDefault();
        if (settings == null)
        {
          settings = new UserSettings();
          settings.UserId = caller.Id;
          if (!hasViewedFeedbackModalOnce.HasValue) hasViewedFeedbackModalOnce = false;
          if (!doNotShowFeedbackModalAgain.HasValue) doNotShowFeedbackModalAgain = false;
          if(!transferredBusinessPlansBannerViewCount.HasValue) transferredBusinessPlansBannerViewCount = 0;
        }
        if (hasViewedFeedbackModalOnce.HasValue) settings.HasViewedFeedbackModalOnce = hasViewedFeedbackModalOnce.Value;
        if (doNotShowFeedbackModalAgain.HasValue) settings.DoNotShowFeedbackModalAgain = doNotShowFeedbackModalAgain.Value;
        if (timezone != null) settings.Timezone = timezone;
        if (!string.IsNullOrEmpty(drawerView)) settings.DrawerView = EnumHelper.ConvertToNullableEnum<gqlDrawerView>(drawerView) ?? default;
        if (transferredBusinessPlansBannerViewCount != null) settings.TransferredBusinessPlansBannerViewCount = transferredBusinessPlansBannerViewCount.Value;
        s.Save(settings);
        s.Flush();

        await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.UpdateUserFeedback(ses, caller));
        transaction.Commit();
        return settings;
      }
    }

    #endregion

  }
}
