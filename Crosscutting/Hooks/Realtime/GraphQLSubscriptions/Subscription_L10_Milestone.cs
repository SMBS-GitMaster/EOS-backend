using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Rocks;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GQL = RadialReview.GraphQL;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_Milestone : IMilestoneHook
  {
    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_Milestone(ITopicEventSender eventSender)
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
    public async Task CreateMilestone(ISession s, Milestone milestone)
    {
      var response = SubscriptionResponse<long>.Added(milestone.Id);

      await _eventSender.SendAsync(ResourceNames.MilestoneEvents, response).ConfigureAwait(false);

      var recurrences = L10Accessor.GetMeetingsContainingGoal(milestone.RockId);

      foreach (var recurrence in recurrences)
      {
        var m = milestone.TransformMilestone();
        
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Created(m.Id, m)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Inserted(Change.Target(m.GoalId, GQL.Models.GoalQueryModel.Collections.Milestone.Milestones), m.Id, m)).ConfigureAwait(false);

        await _eventSender.SendChangeAsync(ResourceNames.Goal(m.GoalId), Change<IMeetingChange>.Created(m.Id, m)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Goal(m.GoalId), Change<IMeetingChange>.Inserted(Change.Target(m.GoalId, GQL.Models.GoalQueryModel.Collections.Milestone.Milestones), m.Id, m)).ConfigureAwait(false);
      }
    }

    public async Task UpdateMilestone(ISession s, UserOrganizationModel caller, Milestone milestone, IMilestoneHookUpdates updates)
    {
      var response = SubscriptionResponse<long>.Updated(milestone.Id);

      await _eventSender.SendAsync(ResourceNames.MilestoneEvents, response).ConfigureAwait(false);

      var recurrences = L10Accessor.GetMeetingsContainingGoal(milestone.RockId);

      foreach(var recurrence in recurrences)
      {
        var m = milestone.TransformMilestone();
        var targets = new []{
          new ContainerTarget
          {
            Type = "goal",
            Id = milestone.RockId,
            Property = "MILESTONES"
          }
        };
        var removedMilestoneChange = Change<IMeetingChange>.Removed(Change.Target(m.Id, GoalQueryModel.Collections.Milestone.Milestones), m.Id, m);
        await _eventSender.SendChangeAsync(ResourceNames.Goal(m.GoalId), removedMilestoneChange).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), removedMilestoneChange).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Updated(m.Id, m, targets)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Goal(m.GoalId), Change<IMeetingChange>.Updated(m.Id, m, targets)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Milestone(m.Id), Change<IMeetingChange>.Updated(m.Id, m, targets)).ConfigureAwait(false);
      }
    }
  }
}
