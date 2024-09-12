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
  public class Subscription_BusinessPlanListItem : IBusinessPlanListItemHook
  {
    private readonly ITopicEventSender _eventSender;

    public Subscription_BusinessPlanListItem(ITopicEventSender eventSender)
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

    public HookPriority GetHookPriority()
    {
      return HookPriority.UI;
    }

    public async Task UpdateBusinessPlanListItem(long businessPlanId, Guid collectionId, BusinessPlanListItem item)
    {
      ContainerTarget[] targets = new ContainerTarget[]
      {
             new ContainerTarget
             {
               Type = "LIST_ITEMS",
               Id = collectionId,
               Property = "TargetOfBusinessPlanListItemCollection"
             },
      };
      await _eventSender
        .SendChangeAsync(
        ResourceNames
        .BusinessPlan(businessPlanId),
        Change<IMeetingChange>.Updated(item.Id, item, targets))
      .ConfigureAwait(false);

    }

    public async Task CreateBusinessPlanListItem(long businessPlanId, Guid collectionId, BusinessPlanListItem item)
    {
      await _eventSender.SendChangeAsync
        (ResourceNames.BusinessPlan(businessPlanId), Change<IMeetingChange>
        .Inserted(Change.Target(collectionId, BusinessPlanListCollectionQueryModel.Collections.BusinessPlanListItemCollection.ListItems), item.Id, item))
        .ConfigureAwait(false);
    }

    public async Task DeleteBusinessPlanListItem(long businessPlanId, Guid collectionId, BusinessPlanListItem itemSubscriptionModel)
    {
      await _eventSender.SendChangeAsync(ResourceNames.BusinessPlan(businessPlanId), Change<IMeetingChange>
        .Removed(Change.Target(collectionId, BusinessPlanListCollectionQueryModel.Collections.BusinessPlanListItemCollection.ListItems), itemSubscriptionModel.Id, itemSubscriptionModel))
        .ConfigureAwait(false);
    }
  }
}
