using HotChocolate.Types;

namespace RadialReview.Core.GraphQL.Models.Mutations
{
  public class UserCreateModel
  {
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    [DefaultValue(null)] public string Timezone { get; set; }
    [DefaultValue(null)] public string Avatar { get; set; }
    [DefaultValue(null)] public long[] Notifications { get; set; }
    [DefaultValue(null)] public long[] Workspaces { get; set; }
    [DefaultValue(null)] public long[] Meetings { get; set; }
    [DefaultValue(null)] public bool? SendInvite { get; set; }
  }

  public class UserEditModel
  {
    public long UserId { get; set; }
    [DefaultValue(null)]
    public string FirstName { get; set; }
    [DefaultValue(null)]
    public string LastName { get; set; }
    [DefaultValue(null)]
    public string Timezone { get; set; }
    [DefaultValue(null)]
    public SettingsUserEditModel Settings { get; set; }
  }

  public class SettingsUserEditModel
  {
    [DefaultValue(null)]
    public bool HasViewedFeedbackModalOnce { get; set; }

    [DefaultValue(null)]
    public bool DoNotShowFeedbackModalAgain { get; set; }
    public int TransferredBusinessPlansBannerViewCount {  get; set; }

    [DefaultValue(null)]
    public long? HomePageMeetingOrWorkspaceId { get; set; }

    [DefaultValue(null)]
    public string HomePageWorkspaceType { get; set; }

  }
}