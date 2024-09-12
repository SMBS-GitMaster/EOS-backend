using RadialReview.Accessors;
using RadialReview.DatabaseModel.Entities;
using RadialReview.Hubs;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;
using System;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.L10
{
  internal class RealTime_L10_Rating : IMeetingRatingHook
  {
    public bool AbsorbErrors()
    {
      return false;
    }

    public bool CanRunRemotely()
    {
      return false;
    }

    public HookPriority GetHookPriority()
    {
      return HookPriority.UI;
    }

    public async Task ShowHiddeRating(RadialReview.Models.UserOrganizationModel caller, L10Recurrence l10Recurrence, bool displayRating)
    {
      PermissionsAccessor.EnsurePermitted(caller, x => x.ViewL10Recurrence(l10Recurrence.Id));
      await using (var rt = RealTimeUtility.Create())
      {
        var meetingHub = rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(l10Recurrence.Id));

        if (displayRating)
        {
          meetingHub.Call("displayRatings");
        } else
        {
          meetingHub.Call("hideRatings");
        }
      }
    }
    public async Task FillUserRating(RadialReview.Models.UserOrganizationModel caller, L10Recurrence l10Recurrence, long userId, HotChocolate.Optional<decimal?> rating)
    {
      PermissionsAccessor.EnsurePermitted(caller, x => x.ViewL10Recurrence(l10Recurrence.Id));
      await using (var rt = RealTimeUtility.Create())
      {
        var meetingHub = rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(l10Recurrence.Id));

        meetingHub.Call("fillUserRating", userId, rating.Value);
      }
    }
  }
}
