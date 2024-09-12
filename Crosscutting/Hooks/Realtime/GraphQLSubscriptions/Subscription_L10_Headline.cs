using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Core.Repositories;
using RadialReview.Core.GraphQL.Types;
using GQL = RadialReview.GraphQL;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_Headline : IHeadlineHook
  {

    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_Headline(ITopicEventSender eventSender)
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

    public async Task CreateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline)
    {
      var response = SubscriptionResponse<long>.Added(headline.Id);
      await _eventSender.SendAsync(ResourceNames.HeadlineEvents, response).ConfigureAwait(false);

      var h = headline.TransformHeadline();
      var meetingId = h.RecurrenceId;

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(meetingId), Change<IMeetingChange>.Created(headline.Id, h)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(meetingId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(headline.Id, GQL.Models.HeadlineQueryModel.Associations.User6.Assignee), headline.Id, h.Assignee)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(meetingId), Change<IMeetingChange>.Inserted(Change.Target(meetingId, GQL.Models.MeetingQueryModel.Collections.Headline.Headlines), headline.Id, h)).ConfigureAwait(false);
    }

    public async Task UpdateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline, IHeadlineHookUpdates updates)
    {
      var response = SubscriptionResponse<long>.Updated(headline.Id);
      await _eventSender.SendAsync(ResourceNames.HeadlineEvents, response).ConfigureAwait(false);

      await SendUpdatedEventOnChannels(headline);
    }

    public async Task ArchiveHeadline(ISession s, PeopleHeadline headline)
    {
      var response = SubscriptionResponse<long>.Archived(headline.Id);

      await _eventSender.SendAsync(ResourceNames.HeadlineEvents, response).ConfigureAwait(false);

      await SendUpdatedEventOnChannels(headline);
    }

    public async Task UnArchiveHeadline(ISession s, PeopleHeadline headline)
    {
      var response = SubscriptionResponse<long>.UnArchived(headline.Id);

      await _eventSender.SendAsync(ResourceNames.HeadlineEvents, response).ConfigureAwait(false);

      await SendUpdatedEventOnChannels(headline);
    }

    private async Task SendUpdatedEventOnChannels(PeopleHeadline headline)
    {
      var h = headline.TransformHeadline();
      var meetingId = h.RecurrenceId;

      var targets = new []{
        new ContainerTarget {
          Type = "meeting",
          Id = meetingId,
          Property = "HEADLINES", // GQL.Models.MeetingModel.Collections.Headline.Headlines.ToString(),
        }
      };

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(meetingId), Change<IMeetingChange>.Updated(headline.Id, h, targets)).ConfigureAwait(false);
      // TODO: NOTE: We're always sending an update message for the Assignee association.  Ideally we should check for a change before sending the message.
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(meetingId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(headline.Id, GQL.Models.HeadlineQueryModel.Associations.User6.Assignee), headline.Id, h.Assignee)).ConfigureAwait(false);      

      await _eventSender.SendChangeAsync(ResourceNames.Headline(headline.Id), Change<IMeetingChange>.Updated(headline.Id, h, targets)).ConfigureAwait(false);
    }
  }
}
