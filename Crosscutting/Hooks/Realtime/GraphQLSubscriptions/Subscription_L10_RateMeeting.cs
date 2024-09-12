using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_RateMeeting : IRateMeetingHooks
  {
    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_RateMeeting(ITopicEventSender eventSender)
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

    public async Task AddRating(ISession session, L10Meeting meeting)
    {
      var response = SubscriptionResponse<long>.Added(meeting.L10RecurrenceId);

      await _eventSender.SendAsync(ResourceNames.RateMeetingEvents, response).ConfigureAwait(false);
    }

    public async Task UpdateRating(ISession session, L10Meeting meeting)
    {
      var response = SubscriptionResponse<long>.Updated(meeting.L10RecurrenceId);

      await _eventSender.SendAsync(ResourceNames.RateMeetingEvents, response).ConfigureAwait(false);
    }
  }
}
