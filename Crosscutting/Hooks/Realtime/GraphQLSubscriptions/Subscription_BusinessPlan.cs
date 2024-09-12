using HotChocolate.Subscriptions;
using RadialReview.BusinessPlan.Core.Data.Models;
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
  public class Subscription_BusinessPlan : IBusinessPlanHook
  {
    private readonly ITopicEventSender _eventSender;

    public Subscription_BusinessPlan(ITopicEventSender eventSender)
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

    public async Task CreateBusinessPlan(BusinessPlanModel businessPlan)
    {
      await _eventSender
        .SendChangeAsync(
          ResourceNames
            .BusinessPlan(businessPlan.Id),
            Change<IMeetingChange>.Created(businessPlan.Id, businessPlan))
        .ConfigureAwait(false);
    }

    public async Task UpdateBusinessPlan(BusinessPlanModel businessPlan)
    {
      await _eventSender
        .SendChangeAsync(
          ResourceNames
            .BusinessPlan(businessPlan.Id),
            Change<IMeetingChange>.Updated(businessPlan.Id, businessPlan, null))
        .ConfigureAwait(false);
    }

    public async Task CreateBusinessPlans(BusinessPlanModel businessPlan)
    {
      await _eventSender
        .SendChangeAsync(
          ResourceNames
            .BusinessPlans,
            Change<IMeetingChange>.Created(businessPlan.Id, businessPlan))
        .ConfigureAwait(false);
    }

    public async Task UpdateBusinessPlans(BusinessPlanModel businessPlan)
    {
      await _eventSender
        .SendChangeAsync(
          ResourceNames
            .BusinessPlans,
            Change<IMeetingChange>.Updated(businessPlan.Id, businessPlan, null))
        .ConfigureAwait(false);
    }
  }
}
