using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.Repositories;
using RadialReview.Models;
using RadialReview.Models.Issues;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Models.L10;
using GQL = RadialReview.GraphQL;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions {
  public class Subscription_L10_CheckIn : ICheckInHook {

    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_CheckIn(ITopicEventSender eventSender) {
      _eventSender = eventSender;
    }

    public bool AbsorbErrors() {
      throw new NotImplementedException();
    }

    public bool CanRunRemotely() {
      throw new NotImplementedException();
    }

    public async Task UpdateCheckIn(ISession s, UserOrganizationModel caller, L10Recurrence.L10Recurrence_Page page, PageCheckInUpdates updates) {
      var response = SubscriptionResponse<long>.Updated(page.Id);

      await _eventSender.SendAsync(ResourceNames.Meeting(page.L10RecurrenceId), response).ConfigureAwait(false);

      var p = page.MeetingPageFromL10RecurrencePage();

      // TODO: Broadcast to all meetings associated with this issue.

      var targets = new[]{
        new ContainerTarget {
          Type = "meeting",
          Id = p.MeetingId,
          Property = "CHECKINS", 
        }
      };

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(page.L10RecurrenceId), Change<IMeetingChange>.Updated(p.Id, p, targets)).ConfigureAwait(false);
    }


    public HookPriority GetHookPriority() {
      throw new NotImplementedException();
    }
  }
}
