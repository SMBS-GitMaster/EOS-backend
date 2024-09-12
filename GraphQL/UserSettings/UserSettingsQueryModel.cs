using System;

namespace RadialReview.GraphQL.Models
{
  public class UserSettingsQueryModel
  {

    #region Properties

    public long Id { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime? DeleteTime { get; set; }

    public long UserId { get; set; }

    public virtual string Timezone { get; set; }

    public virtual string DrawerView {  get; set; }

    public virtual bool HasViewedFeedbackModalOnce { get; set; }

    public virtual bool DoNotShowFeedbackModalAgain { get; set; }

    public virtual int TransferredBusinessPlansBannerViewCount { get; set; }
    public virtual long? WorkspaceHomeId { get; set; }

    public virtual string WorkspaceHomeType { get; set; }

    #endregion

    #region Constructor

    public UserSettingsQueryModel()
    {
      DrawerView = "SLIDE"; // Default value, per Noley, 1/16/24
    }

    #endregion

  }
}
