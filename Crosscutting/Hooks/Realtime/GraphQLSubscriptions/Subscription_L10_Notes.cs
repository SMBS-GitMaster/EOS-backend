using HotChocolate.Subscriptions;
using NHibernate;
using GQL = RadialReview.GraphQL;
using RadialReview.Core.Repositories;
using RadialReview.Core.GraphQL.Types;
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

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_Notes : INoteHook //, IMeetingNoteHook
  {
    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_Notes(ITopicEventSender eventSender)
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

    public async Task CreateNote(ISession s, UserOrganizationModel caller, L10Note note)
    {
      var l10note = note.MeetingNoteFromL10Note();

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(note.Recurrence.Id), Change<IMeetingChange>.Inserted(Change.Target(note.Recurrence.Id, GQL.Models.MeetingQueryModel.Collections.MeetingNote.Notes), l10note.Id, l10note)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(note.Recurrence.Id), Change<IMeetingChange>.Created(l10note.Id, l10note)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(l10note.OwnerId.Value), Change<IMeetingChange>.Created(l10note.Id, l10note)).ConfigureAwait(false);  
      await _eventSender.SendChangeAsync(ResourceNames.User(l10note.OwnerId.Value), Change<IMeetingChange>.UpdatedAssociation(Change.Target(l10note.OwnerId.Value, GQL.Models.MeetingNoteQueryModel.Associations.User4.Owner), l10note.Id, l10note)).ConfigureAwait(false);
    }

    public async Task UpdateNote(ISession s, UserOrganizationModel caller, L10Note note, INoteHookUpdates updates)
    {
      await SendUpdatedEventOnChannels(note, updates);
    }

    private async Task SendUpdatedEventOnChannels(L10Note note,  INoteHookUpdates updates)
    {
      var l10note = note.MeetingNoteFromL10Note();

      var ownerIdNote = l10note.OwnerId.HasValue ? l10note.OwnerId.Value : 0;

      var targets = new[] {
        new ContainerTarget
        {
          Type = "user",
          Id = ownerIdNote,
          Property = "NOTES"
        }
      };
      var targetsMeeting = new[] {
        new ContainerTarget
        {
          Type = "meeting",
          Id = note.Recurrence.Id,
          Property = "NOTES"
        }
      };
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(note.Recurrence.Id), Change<IMeetingChange>.Updated(l10note.Id, l10note, targetsMeeting)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(ownerIdNote), Change<IMeetingChange>.Updated(l10note.Id, l10note, targets)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(ownerIdNote), Change<IMeetingChange>.UpdatedAssociation(Change.Target(ownerIdNote, GQL.Models.MeetingNoteQueryModel.Associations.User4.Owner), l10note.Id, l10note)).ConfigureAwait(false);
    }      
  }
}
