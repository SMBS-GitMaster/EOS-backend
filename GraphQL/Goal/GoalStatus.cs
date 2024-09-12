using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Models.Enums;

namespace RadialReview.GraphQL.Models
{

  public static class GoalStatusExtensions
  {
    public static RockState ToRockState(this gqlGoalStatus goalStatus)
    {
      return
       goalStatus switch
       {
         gqlGoalStatus.COMPLETED => RockState.Complete,
         gqlGoalStatus.ON_TRACK => RockState.OnTrack,
         gqlGoalStatus.OFF_TRACK => RockState.AtRisk,
         _ => RockState.Indeterminate,
       };
    }

    public static RockState? ToRockState(this gqlGoalStatus? goalStatus)
    {
      if (!goalStatus.HasValue)
         return null;

      return goalStatus.Value.ToRockState();
    }
  }
}