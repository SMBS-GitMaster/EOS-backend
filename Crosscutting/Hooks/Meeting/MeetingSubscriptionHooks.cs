using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Core.GraphQL.Types;
using RadialReview.GraphQL.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;
using UserOrganizationModel = RadialReview.Models.UserOrganizationModel;
using RadialReview.Core.Repositories;
using System.Linq.Expressions;
using RadialReview.Accessors;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Spreadsheet;
using RadialReview.Core.GraphQL.MeetingListLookup;

namespace RadialReview.Core.Crosscutting.Hooks.Meeting
{
  public class MeetingSubscriptionHooks : IMeetingEvents
  {
    private readonly ITopicEventSender _eventSender;
    public MeetingSubscriptionHooks(ITopicEventSender eventSender)
    {
      _eventSender = eventSender;
    }

    public HookPriority GetHookPriority()
    {
      return HookPriority.Low;
    }
    public bool AbsorbErrors()
    {
      return true;
    }
    public bool CanRunRemotely()
    {
      return false;
    }

    public async Task StartMeeting(ISession session, L10Recurrence recur, L10Meeting meeting)
    {
      if (meeting.Preview)
        return;

      var response = new GraphQLResponse<StartMeetingMutationOutputDTO>()
      {
        Data = new StartMeetingMutationOutputDTO(MeetingId: meeting.Id, StartTime: meeting.StartTime.Value)
      };

      await _eventSender.SendAsync(ResourceNames.StartMeeting, response);

      var m = meeting.MeetingInstanceFromL10Meeting(meeting.L10RecurrenceId);
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(meeting.L10RecurrenceId), Change<IMeetingChange>.Updated(m.Id, m, null));
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(meeting.L10RecurrenceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(meeting.L10RecurrenceId, MeetingQueryModel.Associations.MeetingInstance2.CurrentMeetingInstance), m.Id, m));
    }


    public async Task AddAttendee(ISession s, long recurrenceId, UserOrganizationModel user, L10Recurrence.L10Recurrence_Attendee attendee)
    {
      //await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Inserted(Change.Target(recurrenceId, MeetingModel.Collections.MeetingAttendee.Attendees), attendee.Id, attendee));

      //var caller = null;
      //var userPerms = RadialReview.Utilities.PermissionsUtility.Create(s, caller);
      var userPerms =  default(MeetingPermissionsModel); // TODO: Replace this default with the correct thing.
      var a = attendee.MeetingAttendeeFromRecurrenceAttendee(recurrenceId);
      var meetingAttendeeLookup = attendee.TransformAttendeeLookup();

      var targets = new[]
          {
            new ContainerTarget
            {
              Type = "meeting",
              Id = recurrenceId,
              Property = "MEETING_ATTENDEES"
            }
          }; // TODO: Add other meetings to which this user belongs.

      await _eventSender.SendChangeAsync(ResourceNames.User(meetingAttendeeLookup.Id), Change<IMeetingChange>.Inserted(Change.Target(recurrenceId, MeetingListLookupModel.Collections.MeetingAttendeeLookup.MeetingAttendeeLookups), meetingAttendeeLookup.Id, meetingAttendeeLookup)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Inserted(Change.Target(recurrenceId, MeetingQueryModel.Collections.MeetingAttendee.Attendees), a.Id, a));
      await _eventSender.SendChangeAsync(ResourceNames.MeetingAttendee(a.Id), Change<IMeetingChange>.Updated(a.Id, a, targets));
    }

    public async Task EditAttendee(ISession s, long recurrenceId, UserOrganizationModel user, L10Recurrence.L10Recurrence_Attendee attendee)
    {
      var userPerms = default(MeetingPermissionsModel); // TODO: Replace this default with the correct thing.
      var a = attendee.MeetingAttendeeFromRecurrenceAttendee(recurrenceId);
      var targets = new[]
          {
            new ContainerTarget
            {
              Type = "meeting",
              Id = recurrenceId,
              Property = "MEETING_ATTENDEES"
            }
          }; // TODO: Add other meetings to which this user belongs.

      var meetingALookupTarget = new[]
      {
        new ContainerTarget
        {
          Type = "meeting",
          Id = recurrenceId,
          Property = "MEETING_ATTENDEE_LOOKUPS"
        }
      };

      var meetingAttendeeLookup = attendee.TransformAttendeeLookup();

      await _eventSender.SendChangeAsync(ResourceNames.User(meetingAttendeeLookup.Id), Change<IMeetingChange>.Updated(meetingAttendeeLookup.Id, meetingAttendeeLookup, meetingALookupTarget));
      await _eventSender.SendChangeAsync(ResourceNames.MeetingAttendee(a.Id), Change<IMeetingChange>.Updated(a.Id, a, targets));
    }

    public async Task ConcludeMeeting(ISession s, UserOrganizationModel caller, L10Recurrence recur, L10Meeting meeting)
    {
      var response = SubscriptionResponse<long>.Updated(meeting.L10RecurrenceId);

      await _eventSender.SendAsync(ResourceNames.WrapUpEvents, response);

      var m = meeting.MeetingInstanceFromL10Meeting(meeting.L10RecurrenceId);
      var meetingQueryModel = RepositoryTransformers.MeetingFromRecurrence(recur, caller, null, null);

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(meeting.L10RecurrenceId), Change<IMeetingChange>.Updated(meetingQueryModel.Id, meetingQueryModel, null));
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(meeting.L10RecurrenceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(meeting.L10RecurrenceId, MeetingQueryModel.Associations.MeetingInstance2.CurrentMeetingInstance), m.Id, default(MeetingInstanceQueryModel)));
      }
    public async Task CreateRecurrence(ISession s, L10Recurrence recur)
    {
      //noop
    }
    public async Task DeleteMeeting(ISession s, L10Meeting meeting)
    {
      //noop
    }
    public async Task DeleteRecurrence(ISession s, L10Recurrence recur)
    {
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recur.Id), Change<IMeetingChange>.Deleted<MeetingQueryModel>(recur.Id));
    }

    public async Task UpdateRecurrence(ISession s, UserOrganizationModel caller, L10Recurrence recurrence)
    {
      var favorite = FavoriteAccessor.GetFavoriteForUser(caller, RadialReview.Models.FavoriteType.Meeting, recurrence.Id);
      var settings = MeetingSettingsAccessor.GetSettingsForMeeting(caller, recurrence.Id);

      // NOTE: We have to reload the recurrence because the transformer method relies on some underscore propertiies being non-null (unfortunately)!
      var loadMeeting = new LoadMeeting { LoadAudio = false, LoadMeasurables = false, LoadNotes = false, LoadPages = true, LoadRocks = false, LoadUsers = true, LoadVideos = false, LoadConclusionActions = true };
      var source = L10Accessor.GetL10Recurrence(caller, recurrence.Id, loadMeeting);
      var m = source.MeetingFromRecurrence(caller, favorite, settings);

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Updated(m.Id, m, null));
      await _eventSender.SendChangeAsync(ResourceNames.Meetings, Change<IMeetingChange>.Updated(m.Id, m, null));
    }

    public async Task RemoveAttendee(ISession s, long recurrenceId, long userId, List<L10Recurrence.L10Recurrence_Attendee> removedFromRecurrence, List<L10Meeting.L10Meeting_Attendee> removedFromMeeting)
    {
      foreach (var item in removedFromRecurrence)
      {
        var id = item.L10Recurrence.Id;
        var a = item.MeetingAttendeeFromRecurrenceAttendee(id);
        var meetingAttendeeLookup = item.TransformAttendeeLookup();
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(id), Change<IMeetingChange>.Removed(Change.Target(id, MeetingQueryModel.Collections.MeetingAttendee.Attendees), userId, a)).ConfigureAwait(false);

        var recurrence = item.L10Recurrence;
        var caller = item.User;

        var m = recurrence.MeetingFromRecurrence(item.User, null, null);
        await _eventSender.SendChangeAsync(ResourceNames.User(userId), Change<IMeetingChange>.Removed(Change.Target(id, UserQueryModel.Collections.Meeting3.Meetings), m.Id, m)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.User(meetingAttendeeLookup.Id), Change<IMeetingChange>.Removed(Change.Target(id, MeetingListLookupModel.Collections.MeetingAttendeeLookup.MeetingAttendeeLookups), meetingAttendeeLookup.Id, meetingAttendeeLookup)).ConfigureAwait(false);
      }
    }

    public async Task UpdateUserFeedback(ISession session, UserOrganizationModel user)
    {
      UserQueryModel userQueryModel = UserTransformer.TransformUser(user);
      var change = Change<IMeetingChange>.Updated(user.Id, userQueryModel, new[]{
      new ContainerTarget {
            Type = "user",
            Id = user.Id,
            Property = "USERS"
          }}
      );
      await _eventSender.SendChangeAsync(ResourceNames.User(user.Id), change).ConfigureAwait(false);
    }
    public async Task UndeleteRecurrence(ISession s, L10Recurrence recur)
    {
      var m = recur.MeetingInstanceFromRecurrence();
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recur.Id), Change<IMeetingChange>.Updated(m.Id, m, null));
    }

    public async Task UpdateCurrentMeetingInstance(ISession s, UserOrganizationModel caller, L10Recurrence recurrence)
    {
      long recurrenceId = recurrence.Id;
      var m = recurrence.L10MeetingInProgress.MeetingInstanceFromL10Meeting(recurrenceId);
      var meetingTargets = new[]{
        new ContainerTarget {
          Type = "meeting",
          Id = recurrenceId,
          Property = "MEETING_INSTANCES", // RadialReview.GraphQL.Models.MeetingModel.Collections.MeetingInstance.MeetingInstances.ToString(),
        },
        new ContainerTarget {
          Type = "meeting",
          Id = recurrenceId,
          Property = "CURRENT_MEETING_INSTANCE", // RadialReview.GraphQL.Models.MeetingModel.Collections.MeetingInstance.MeetingInstances.ToString(),
        }
      };
      var meetingChange = Change<IMeetingChange>.Updated(m.Id, m, meetingTargets);

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), meetingChange);

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(recurrenceId, MeetingQueryModel.Associations.MeetingInstance2.CurrentMeetingInstance), m.Id, m));
    }

    public async Task CurrentPageChanged(ISession s, long recurrenceId, L10Meeting meeting, string pageName, double now_ms, double baseMins)
    {
      if (meeting.Preview)
        return;

      var m = meeting.MeetingInstanceFromL10Meeting(recurrenceId);
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Updated(m.Id, m, new[]{
        new ContainerTarget {
          Type = "meeting",
          Id = recurrenceId,
          Property = "MEETING_INSTANCES", // RadialReview.GraphQL.Models.MeetingModel.Collections.MeetingInstance.MeetingInstances.ToString(),
        },
        new ContainerTarget {
          Type = "meeting",
          Id = recurrenceId,
          Property = "CURRENT_MEETING_INSTANCE", // RadialReview.GraphQL.Models.MeetingModel.Collections.MeetingInstance.MeetingInstances.ToString(),
        }
      }));

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(recurrenceId, MeetingQueryModel.Associations.MeetingInstance2.CurrentMeetingInstance), m.Id, m));
    }

    public async Task CreatePage(ISession s, UserOrganizationModel caller, L10Recurrence.L10Recurrence_Page page)
    {
      var p = page.MeetingPageFromL10RecurrencePage();
      var favorite = FavoriteAccessor.GetFavoriteForUser(caller, RadialReview.Models.FavoriteType.Meeting, p.MeetingId);
      var settings = MeetingSettingsAccessor.GetSettingsForMeeting(caller, p.MeetingId);
      var m = page.L10Recurrence.MeetingFromRecurrence(caller, favorite, settings);

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(m.Id), Change<IMeetingChange>.Inserted(Change.Target(m.Id, MeetingQueryModel.Collections.MeetingPage.MeetingPages), p.Id, p)).ConfigureAwait(false);
    }

    public async Task UpdatePage(ISession s, UserOrganizationModel caller, L10Recurrence.L10Recurrence_Page page)
    {
      var p = page.MeetingPageFromL10RecurrencePage();
      var favorite = FavoriteAccessor.GetFavoriteForUser(caller, RadialReview.Models.FavoriteType.Meeting, p.MeetingId);
      var settings = MeetingSettingsAccessor.GetSettingsForMeeting(caller, p.MeetingId);
      var m = page.L10Recurrence.MeetingFromRecurrence(caller, favorite, settings);

      var targets = new ContainerTarget[]
      {
        new()
        {
          Type = "meeting",
          Id   = m.Id,
          Property = "MEETING_PAGES"
        }
      };

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(m.Id), Change<IMeetingChange>.Updated(p.Id, p, targets)).ConfigureAwait(false);
    }

    public async Task RemovePage(ISession s, UserOrganizationModel caller, L10Recurrence.L10Recurrence_Page page)
    {
      var p = page.MeetingPageFromL10RecurrencePage();
      var favorite = FavoriteAccessor.GetFavoriteForUser(caller, RadialReview.Models.FavoriteType.Meeting, p.MeetingId);
      var settings = MeetingSettingsAccessor.GetSettingsForMeeting(caller, p.MeetingId);
      var m = page.L10Recurrence.MeetingFromRecurrence(caller, favorite, settings);

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(m.Id), Change<IMeetingChange>.Removed(Change.Target(m.Id, MeetingQueryModel.Collections.MeetingPage.MeetingPages), p.Id, p)).ConfigureAwait(false);
    }
  }
}
