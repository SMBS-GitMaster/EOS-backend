using HotChocolate.Subscriptions;
using RadialReview.BusinessPlan.Core.Data.Models;
using RadialReview.BusinessPlan.Models;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_BusinessPlanTile: IBusinessPlanTileHook
  {
    private readonly ITopicEventSender _eventSender;

    public Subscription_BusinessPlanTile(ITopicEventSender eventSender)
    {
      _eventSender = eventSender;
    }

    public bool AbsorbErrors()
    {
      return false;
    }

    public bool CanRunRemotely()
    {
      return false;
    }

    public async Task CreateBusinessPlanTile(long businessPlanId, BusinessPlanTile tile)
    {
      await _eventSender
        .SendChangeAsync(ResourceNames.BusinessPlan(businessPlanId), Change<IMeetingChange>
        .Inserted(Change.Target(businessPlanId, BusinessPlanModel.Collections.BusinessPlanTileBusiness.BusinessPlanTileBusiness), businessPlanId, tile))
        .ConfigureAwait(false);

    }

    public Task DeleteBusinessPlanTile(long businessPlanId, BusinessPlanTile tile)
    {
      throw new NotImplementedException();
    }

    public HookPriority GetHookPriority()
    {
      return HookPriority.UI;
    }

    public async Task UpdateBusinessPlanTile(long businessPlanId, BusinessPlanTile tile)
    {
          await _eventSender
            .SendChangeAsync(
            ResourceNames
            .BusinessPlan(businessPlanId),
            Change<IMeetingChange>.Updated(tile.Id, tile, null))
        .ConfigureAwait(false);
    }
  }
}
