using DocumentFormat.OpenXml.Drawing;
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
  public class Subscription_BusinessPlanListCollection : IBusinessPlanListCollectionHook
  {
    private readonly ITopicEventSender _eventSender;

    public Subscription_BusinessPlanListCollection(ITopicEventSender eventSender)
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

    public async Task CreateBusinessPlanListCollection(long businessPlanId, Guid tileId, BusinessPlanListCollection collection)
    {
      await _eventSender
      .SendChangeAsync(ResourceNames.BusinessPlan(businessPlanId), Change<IMeetingChange>
      .Inserted(Change.Target(tileId, BusinessPlanModel.Collections.BusinessPlanListCollectionTile.BusinessPlanListCollectionTile), collection.Id, collection))
      .ConfigureAwait(false);
    }

    public HookPriority GetHookPriority()
    {
      return HookPriority.UI;
    }

    public async Task UpdateBusinessPlanListCollection(long businessPlanId, BusinessPlanListCollection collection)
    {
      await _eventSender
      .SendChangeAsync(
           ResourceNames
           .BusinessPlan(businessPlanId),
           Change<IMeetingChange>.Updated(collection.Id, collection, null))
       .ConfigureAwait(false);
    }
  }
}
