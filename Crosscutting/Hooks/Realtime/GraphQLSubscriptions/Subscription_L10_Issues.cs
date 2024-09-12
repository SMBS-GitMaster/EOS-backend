using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.Repositories;
using RadialReview.Models;
using RadialReview.Models.Issues;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;
using GQL = RadialReview.GraphQL;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_Issues : IIssueHook
  {
    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_Issues(ITopicEventSender eventSender)
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

    public async Task CreateIssue(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence issue)
    {
      var response = SubscriptionResponse<long>.Added(issue.Id);

      await _eventSender.SendAsync(ResourceNames.IssueEvents, response).ConfigureAwait(false);

      var i = issue.IssueFromIssueRecurrence();
      i.AddToDepartmentPlan = issue.AddToDepartmentPlan;
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(i.RecurrenceId), Change<IMeetingChange>.Created(i.Id, i)).ConfigureAwait(false);
      // TODO: Broadcast the message to all meetings associated with this issue.s


      // Add newer "longTermIssues" notification
      if (i.AddToDepartmentPlan)
      {
        var targets = new[]{
        new ContainerTarget {
          Type = "meeting",
          Id = i.RecurrenceId,
          Property = "LONG_TERM_ISSUES",
        }
      };

        await _eventSender.SendChangeAsync(ResourceNames.Issue(i.RecurrenceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(i.RecurrenceId, GQL.Models.IssueQueryModel.Associations.Issue2.Issue), i.Id, i)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(i.RecurrenceId), Change<IMeetingChange>.Inserted(Change.Target(i.RecurrenceId, GQL.Models.MeetingQueryModel.Collections.Issue.LongTermIssues), i.Id, i)).ConfigureAwait(false);

      }
      else
      {
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(i.RecurrenceId), Change<IMeetingChange>.Inserted(Change.Target(i.RecurrenceId, GQL.Models.MeetingQueryModel.Collections.Issue.Issues), i.Id, i)).ConfigureAwait(false);
      }
    }

    public async Task SendIssueTo(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence sourceIssue, IssueModel.IssueModel_Recurrence destIssue)
    {
      var source = sourceIssue.IssueFromIssueRecurrence();
      var dest = destIssue.IssueFromIssueRecurrence();

      source.SentToIssue = dest;

      if (source.SentToIssueId == null || source.sentToIssueMeetingName == null)
      {
        source.SentToIssueId = destIssue.Id;
        source.sentToIssueMeetingName = destIssue.Recurrence.Name;
      }

      if (dest.SentFromIssueId == null)
      {
        dest.SentFromIssueId = source.Id;
      }

      var targetSource = new[]{
        new ContainerTarget {
          Type = "meeting",
          Id = source.RecurrenceId,
          Property = "ISSUES",
        },
        new ContainerTarget {
          Type = "meeting",
          Id = source.RecurrenceId,
          Property = "LONG_TERM_ISSUES",
        },
         new ContainerTarget {
          Type = "meeting",
          Id = source.RecurrenceId,
          Property = "RECENTLY_SOLVED_ISSUES",
        },
          new ContainerTarget {
          Type = "meeting",
          Id = source.RecurrenceId,
          Property = "SENT_TO_ISSUES",
        },
      };

      var targetDest = new[]{
        new ContainerTarget {
          Type = "meeting",
          Id = dest.RecurrenceId,
          Property = "ISSUES",
        }
      };

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(source.RecurrenceId), Change<IMeetingChange>.Updated(source.Id, source, targetSource)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(dest.RecurrenceId), Change<IMeetingChange>.Updated(dest.Id, dest, targetDest)).ConfigureAwait(false);

      await _eventSender.SendChangeAsync(ResourceNames.Issue(source.Id), Change<IMeetingChange>.Updated(source.Id, source, null)).ConfigureAwait(false);

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(source.RecurrenceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(source.RecurrenceId, GQL.Models.IssueQueryModel.Associations.Issue2.Issue), source.Id, source)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(dest.RecurrenceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(dest.RecurrenceId, GQL.Models.IssueQueryModel.Associations.Issue2.Issue), dest.Id, dest)).ConfigureAwait(false);

    }

    public async Task UpdateIssue(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence issue, IIssueHookUpdates updates)
    {
      var response = SubscriptionResponse<long>.Updated(issue.Id);

      await _eventSender.SendAsync(ResourceNames.IssueEvents, response).ConfigureAwait(false);
      await SendUpdatedEventOnChannels(issue, updates.AddToDepartmentPlan, updates);
    }

    private async Task SendUpdatedEventOnChannels(IssueModel.IssueModel_Recurrence issue, bool addToDepartmentPlan, IIssueHookUpdates updates = null)
    {
      if (updates is null)
        updates = new IIssueHookUpdates();

      var i = issue.IssueFromIssueRecurrence();
      i.AddToDepartmentPlan = addToDepartmentPlan;
      var meetingId = i.RecurrenceId;

      // TODO: Broadcast to all meetings associated with this issue.
      // This one can be out of date if raised from a sentTo...the new id doesn't pop.

      var targets = new[]{
        new ContainerTarget {
          Type = "meeting",
          Id = meetingId,
          Property = "ISSUES",
        },
        new ContainerTarget {
          Type = "meeting",
          Id = meetingId,
          Property = "LONG_TERM_ISSUES",
        },
         new ContainerTarget {
          Type = "meeting",
          Id = meetingId,
          Property = "RECENTLY_SOLVED_ISSUES",
        },
          new ContainerTarget {
          Type = "meeting",
          Id = meetingId,
          Property = "SENT_TO_ISSUES",
        },
      };

      if (updates.AddToDepartmentPlanChanged)
      {
        if (updates.AddToDepartmentPlan)
        {
          // Removed from short-term, added to long-term
          await _eventSender.SendChangeAsync(ResourceNames.Meeting(i.RecurrenceId), Change<IMeetingChange>.Removed(Change.Target(i.RecurrenceId, GQL.Models.MeetingQueryModel.Collections.Issue.Issues), i.Id, i)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.Meeting(i.RecurrenceId), Change<IMeetingChange>.Inserted(Change.Target(i.RecurrenceId, GQL.Models.MeetingQueryModel.Collections.Issue.LongTermIssues), i.Id, i)).ConfigureAwait(false);
        }
        else
        {
          // Removed from long-term, added to short-term
          await _eventSender.SendChangeAsync(ResourceNames.Meeting(i.RecurrenceId), Change<IMeetingChange>.Inserted(Change.Target(i.RecurrenceId, GQL.Models.MeetingQueryModel.Collections.Issue.Issues), i.Id, i)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.Meeting(i.RecurrenceId), Change<IMeetingChange>.Removed(Change.Target(i.RecurrenceId, GQL.Models.MeetingQueryModel.Collections.Issue.LongTermIssues), i.Id, i)).ConfigureAwait(false);
        }
      }

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(meetingId), Change<IMeetingChange>.Updated(i.Id, i, targets)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Issue(issue.Id), Change<IMeetingChange>.Updated(i.Id, i, targets)).ConfigureAwait(false);

      await _eventSender.SendChangeAsync(ResourceNames.Issue(meetingId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(meetingId, GQL.Models.IssueQueryModel.Associations.Issue2.Issue), i.Id, i)).ConfigureAwait(false);
    }
  }
}
