using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.GraphQL.Models.IssueQueryModel.Associations;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_MeetingAttendee : IMeetingAttendeeHooks
  {

    #region Fields

    private readonly ITopicEventSender _eventSender;

    #endregion

    #region Constructor

    public Subscription_MeetingAttendee(ITopicEventSender eventSender)
    {
      _eventSender = eventSender;
    }

    #endregion

    #region Public Methods

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

    public async Task UpdateAttendee(ISession session, UserOrganizationModel caller, long recurrenceId, L10Recurrence.L10Recurrence_Attendee attendee)
    {
      var response = SubscriptionResponse<long>.Updated(attendee.User.Id);
      await _eventSender.SendAsync(ResourceNames.MeetingAttendeeEvents, response).ConfigureAwait(false);

      var targets = new[]
      {
        new ContainerTarget
        {
          Type = "meeting",
          Id = recurrenceId,
          Property = "MEETING_ATTENDEES"
        },
        new ContainerTarget
        {
          Type = "meetingInstance",
          Id = recurrenceId,
          Property = "MEETING_ATTENDEES"
        },
        new ContainerTarget
        {
          Type = "currentMeetingInstance",
          Id = recurrenceId,
          Property = "MEETING_ATTENDEES"
        }
      }; // TODO: Add other meetings to which this user belongs.

      var a = attendee.MeetingAttendeeFromRecurrenceAttendee(recurrenceId);

      await _eventSender.SendChangeAsync(ResourceNames.MeetingAttendee(attendee.User.Id), Change<IMeetingChange>.Updated(attendee.User.Id, a, targets)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Updated(attendee.User.Id, a, targets)).ConfigureAwait(false);

      var m = L10Accessor.GetCurrentL10Meeting(caller, recurrenceId);
      var mi = MeetingInstanceTransformer.MeetingInstanceFromL10Meeting(m, recurrenceId);
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Updated(mi.Id, mi, targets)).ConfigureAwait(false);

    }

    #endregion

  }
}
