namespace RadialReview.GraphQL.Models.Mutations
{
  public class UserSettingsEditModel
  {

    #region Properties

    public bool? HasViewedFeedbackModalOnce { get; set; }

    public bool? DoNotShowFeedbackModalAgain { get; set; }

    public string Timezone { get; set; }

    public string DrawerView {  get; set; }
    public int? TransferredBusinessPlansBannerViewCount {  get; set; }

    #endregion

  }
}
