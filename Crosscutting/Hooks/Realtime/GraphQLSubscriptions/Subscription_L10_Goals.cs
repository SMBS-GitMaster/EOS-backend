using HotChocolate.Subscriptions;
using NHibernate;
using GQL = RadialReview.GraphQL;
using RadialReview.Repositories;
using RadialReview.Core.Repositories;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.GraphQL.Models;
using static RadialReview.GraphQL.Models.MeetingQueryModel.Collections;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_Goals : IRockHook, IMeetingRockHook
  {
    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_Goals(ITopicEventSender eventSender)
    {
      _eventSender = eventSender;
    }

    public bool CanRunRemotely()
    {
      return false;
    }

    public bool AbsorbErrors()
    {
      return false;
    }

    public HookPriority GetHookPriority()
    {
      return HookPriority.UI;
    }

    public async Task CreateRock(ISession s, UserOrganizationModel caller, RockModel rock)
    {
      var response = SubscriptionResponse<long>.Added(rock.Id);

      await _eventSender.SendAsync(ResourceNames.GoalsEvents, response);

      //await _eventSender.SendAsync(ResourceNames.Goal(rock.Id));

      var recurrencesInGoal = RockAccessor.GetGoalRecurrenceRecords_Unsafe(rock.Id);
      rock._GoalRecurenceRecords = recurrencesInGoal;

      foreach (var recurrence in recurrencesInGoal)
      {
        var goal = rock.TransformRock(recurrence.RecurrenceId);

        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.RecurrenceId), Change<IMeetingChange>.Created(goal.Id, goal)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.RecurrenceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(goal.Id, GQL.Models.GoalQueryModel.Associations.User5.Assignee), goal.Assignee.Id, goal.Assignee)).ConfigureAwait(false);

        await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.Created(goal.Id, goal)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(goal.Id, GQL.Models.GoalQueryModel.Associations.User5.Assignee), goal.Assignee.Id, goal.Assignee)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.Inserted(Change.Target(goal.Id, GQL.Models.UserQueryModel.Collections.UserGoal.Goal), goal.Id, goal)).ConfigureAwait(false);
      }
    }

    public async Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates)
    {
      var response = SubscriptionResponse<long>.Updated(rock.Id);

      var recurrencesInGoal = RockAccessor.GetGoalRecurrenceRecords_Unsafe(rock.Id);
      rock._GoalRecurenceRecords = recurrencesInGoal;

      await _eventSender.SendAsync(ResourceNames.GoalsEvents, response).ConfigureAwait(false);
      await SendUpdatedEventOnChannels(rock, updates);
    }

    public async Task ArchiveRock(ISession s, RockModel rock, bool deleted)
    {
      var response = SubscriptionResponse<long>.Archived(rock.Id);

      await _eventSender.SendAsync(ResourceNames.GoalsEvents, response).ConfigureAwait(false);
      await SendUpdatedEventOnChannels(rock, null);
    }

    public async Task UnArchiveRock(ISession s, RockModel rock, bool v)
    {
      var response = SubscriptionResponse<long>.UnArchived(rock.Id);

      var recurrencesInGoal = RockAccessor.GetGoalRecurrenceRecords_Unsafe(rock.Id);
      rock._GoalRecurenceRecords = recurrencesInGoal;

      await _eventSender.SendAsync(ResourceNames.GoalsEvents, response).ConfigureAwait(false);
      await SendUpdatedEventOnChannels(rock, null);
    }

    public async Task DetachRock(ISession s, RockModel rock, long recurrenceId, IMeetingRockHookUpdates updates)
    {
      var response = SubscriptionResponse<long>.Archived(rock.Id);

      await _eventSender.SendAsync(ResourceNames.GoalsEvents, response).ConfigureAwait(false);

      var recurrences = L10Accessor.GetMeetingsContainingGoal(rock.Id);
      var removedRecurrence = L10Accessor.GetRemovedRecurrenceInGoal(rock.Id, recurrenceId, updates.DeleteTime.Value);
      var removedGoalRecurrenceRecord = RockAccessor.GetRemovedGoalRecurrenceRecord_Unsafe( rock.Id, recurrenceId, updates.DeleteTime.Value );

      var goalRecurrenceRecords = RockAccessor.GetGoalRecurrenceRecords_Unsafe(rock.Id);
      rock._GoalRecurenceRecords = goalRecurrenceRecords;

      var goal = rock.TransformRock(recurrenceId);
      var removedDeparmentPlanRecord = removedGoalRecurrenceRecord.ToGoalDepartmentPlanRecordQueryModel();

      var allRecurrences = recurrences.ToList();
      allRecurrences.Add(removedRecurrence);
      foreach (var recurrence in allRecurrences)
      {
        goal.RecurrenceId = recurrence.Id;
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(goal.RecurrenceId), Change<IMeetingChange>.Removed(Change.Target(goal.RecurrenceId, GQL.Models.MeetingQueryModel.Collections.Goal.Goals), goal.Id, goal)).ConfigureAwait(false);

        //Getting MeetingQueryModel to send in Channels
        FavoriteModel favorite = FavoriteAccessor.GetFavoriteForUser(rock.AccountableUser, FavoriteType.Meeting, goal.RecurrenceId);
        MeetingSettingsModel settings = MeetingSettingsAccessor.GetSettingsForMeeting(rock.AccountableUser, goal.RecurrenceId);
        MeetingQueryModel m = recurrence.MeetingFromRecurrence(rock.AccountableUser, favorite, settings);
        GoalMeetingQueryModel gm = GoalMeetingQueryModel.FromMeetingQueryModel(m, goal.Id);

        await _eventSender.SendChangeAsync(ResourceNames.Goal(goal.Id), Change<IMeetingChange>.Removed(Change.Target(goal.Id, GQL.Models.GoalQueryModel.Collections.Meeting4.Meetings), gm.Id, gm)).ConfigureAwait(false);

        await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.Removed(Change.Target(m.Id, GQL.Models.MeetingQueryModel.Collections.Goal.Goals), goal.Id, goal)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.Removed(Change.Target(goal.Id, GQL.Models.GoalQueryModel.Collections.Meeting4.Meetings), gm.Id, gm)).ConfigureAwait(false);

        await _eventSender.SendChangeAsync(ResourceNames.Goal(goal.Id), Change<IMeetingChange>.Removed(Change.Target(goal.Id, GQL.Models.GoalQueryModel.Collections.DepartmentPlanRecord.DepartmentPlanRecords), removedDeparmentPlanRecord.Id, removedDeparmentPlanRecord)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(goal.RecurrenceId), Change<IMeetingChange>.Removed(Change.Target(goal.RecurrenceId, GQL.Models.GoalQueryModel.Collections.DepartmentPlanRecord.DepartmentPlanRecords), removedDeparmentPlanRecord.Id, removedDeparmentPlanRecord)).ConfigureAwait(false);

        // Personal goals
        var targets = new[]{
          new ContainerTarget {
            Type = "user",
            Id = goal.Id,
            Property = "GOALS"
          },
        };

        await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(goal.Id, GQL.Models.UserQueryModel.Associations.UserGoals.Goals), goal.Id, goal)).ConfigureAwait(false);
      }
    }

    public async Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock)
    {
      var response = SubscriptionResponse<long>.Updated(recurRock.Id);

      await _eventSender.SendAsync(ResourceNames.GoalsEvents, response).ConfigureAwait(false);

      var recurrencesInGoal = RockAccessor.GetGoalRecurrenceRecords_Unsafe(recurRock.ForRock.Id);
      recurRock._GoalRecurrenceRecords = recurrencesInGoal;
      recurRock.ForRock._GoalRecurenceRecords = recurrencesInGoal;

      var goalRecurrenceUpdated = recurRock._GoalRecurrenceRecords.Where(x => x.RecurrenceRockId == recurRock.Id).FirstOrDefault();
      var deparmentPlanRecordUpdated = goalRecurrenceUpdated.ToGoalDepartmentPlanRecordQueryModel();

      foreach (var recurrence in recurrencesInGoal)
      {
        var goal = recurRock.ForRock.TransformRock(recurrence.RecurrenceId);
        var targets = new [] {
          new ContainerTarget {
            Type = "meeting",
            Id = recurrence.RecurrenceId,
            Property = "GOALS"
          },

          new ContainerTarget {
            Type = "goal",
            Id = goal.Id, 
            Property = "MEETINGS"
          }
        };

        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.RecurrenceId), Change<IMeetingChange>.Updated(goal.Id, goal, targets)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.RecurrenceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(goal.Id, GQL.Models.GoalQueryModel.Associations.User5.Assignee), goal.Assignee.Id, goal.Assignee)).ConfigureAwait(false);

        await _eventSender.SendChangeAsync(ResourceNames.Goal(goal.Id), Change<IMeetingChange>.Updated(goal.Id, goal, targets)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Goal(goal.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(goal.Id, GQL.Models.GoalQueryModel.Associations.User5.Assignee), goal.Assignee.Id, goal.Assignee)).ConfigureAwait(false);

        await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.Updated(goal.Id, goal, targets)).ConfigureAwait(false);

        await _eventSender.SendChangeAsync(ResourceNames.Goal(recurRock.ForRock.Id), Change<IMeetingChange>.Updated(deparmentPlanRecordUpdated.Id, deparmentPlanRecordUpdated, targets)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.RecurrenceId), Change<IMeetingChange>.Updated(deparmentPlanRecordUpdated.Id, deparmentPlanRecordUpdated, targets)).ConfigureAwait(false);
      }
    }

    public async Task UndeleteRock(ISession s, RockModel rock)
    {
      await SendUpdatedEventOnChannels(rock, null);
    }

    public async Task AttachRock(ISession s, UserOrganizationModel caller, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock)
    {
      var recurrenceId = recurRock.L10Recurrence.Id;

      var recurrences = L10Accessor.GetMeetingsContainingGoal(rock.Id);

      var goalRecurrenceRecords = RockAccessor.GetGoalRecurrenceRecords_Unsafe(recurRock.ForRock.Id);
      recurRock._GoalRecurrenceRecords = goalRecurrenceRecords;
      recurRock.ForRock._GoalRecurenceRecords = goalRecurrenceRecords;

      var goalRecurrenceAdded = recurRock._GoalRecurrenceRecords.Where(x => x.RecurrenceRockId == recurRock.Id).FirstOrDefault();
      var deparmentPlanRecordAdded = goalRecurrenceAdded.ToGoalDepartmentPlanRecordQueryModel();

      foreach (var recurrence in recurrences)
      {
        var goal = rock.TransformRock(recurrence.Id);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Inserted(Change.Target(recurrence.Id, GQL.Models.MeetingQueryModel.Collections.Goal.Goals), goal.Id, goal)).ConfigureAwait(false);

        var m = recurrence.MeetingInstanceFromRecurrence();
        if (m is not null)
        {
          GoalMeetingQueryModel gm = GoalMeetingQueryModel.FromMeetingInstanceQueryModel(m, goal.Id);
          await _eventSender.SendChangeAsync(ResourceNames.Goal(rock.Id), Change<IMeetingChange>.Inserted(Change.Target(goal.Id, GQL.Models.GoalQueryModel.Collections.Meeting4.Meetings), gm.Id, gm)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.Inserted(Change.Target(goal.Id, GQL.Models.GoalQueryModel.Collections.Meeting4.Meetings), gm.Id, gm)).ConfigureAwait(false);
        }

        await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.Inserted(Change.Target(m.Id, GQL.Models.MeetingQueryModel.Collections.Goal.Goals), goal.Id, goal)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Goal(goal.Id), Change<IMeetingChange>.Inserted(Change.Target(goal.Id, GQL.Models.GoalQueryModel.Collections.DepartmentPlanRecord.DepartmentPlanRecords), deparmentPlanRecordAdded.Id, deparmentPlanRecordAdded)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Inserted(Change.Target(recurrence.Id, GQL.Models.GoalQueryModel.Collections.DepartmentPlanRecord.DepartmentPlanRecords), deparmentPlanRecordAdded.Id, deparmentPlanRecordAdded)).ConfigureAwait(false);


        // Personal goals
        var targets = new[]{
          new ContainerTarget {
            Type = "user",
            Id = goal.Id,
            Property = "GOALS"
          },
        };

        await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(goal.Id, GQL.Models.UserQueryModel.Associations.UserGoals.Goals), goal.Id, goal)).ConfigureAwait(false);
      }
    }

    private async Task SendUpdatedEventOnChannels(RockModel rock, IRockHookUpdates updates)
    {
      var recurrencesInGoal = RockAccessor.GetGoalRecurrenceRecords_Unsafe(rock.Id);
      rock._GoalRecurenceRecords = recurrencesInGoal;

      if (recurrencesInGoal.Count == 0)
      {
        // Personal Goal
        var goal = rock.TransformRock(null);

        var targets = new[]{
          new ContainerTarget {
            Type = "user",
            Id = goal.Id,
            Property = "GOALS"
          },
        };

        if (updates != null && updates.AccountableUserChanged)
        {
          // User changed OR deleted
          await _eventSender.SendChangeAsync(ResourceNames.User(updates.OriginalAccountableUserId), Change<IMeetingChange>.Removed(Change.Target(goal.Id, GQL.Models.UserQueryModel.Collections.UserGoal.Goal), goal.Id, goal)).ConfigureAwait(false);
        }

        await _eventSender.SendChangeAsync(ResourceNames.Goal(goal.Id), Change<IMeetingChange>.Updated(goal.Id, goal, targets)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Goal(goal.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(goal.Id, GQL.Models.GoalQueryModel.Associations.User5.Assignee), goal.Assignee.Id, goal.Assignee)).ConfigureAwait(false);

        await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.Updated(goal.Id, goal, targets)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(goal.Id, GQL.Models.UserQueryModel.Associations.UserGoals.Goals), goal.Id, goal)).ConfigureAwait(false);
      }
      else
      {
        foreach (var recurrence in recurrencesInGoal)
        {
          var goal = rock.TransformRock(recurrence.RecurrenceId);

          var targets = new[]{
          new ContainerTarget {
            Type = "meeting",
            Id = recurrence.RecurrenceId,
            Property = "GOALS", // GQL.Models.MeetingModel.Collections.Goal.Goals.ToString(),
          },

          new ContainerTarget {
            Type = "goal",
            Id = goal.Id,
            Property = "MEETINGS"
          },
          new ContainerTarget {
            Type = "user",
            Id = goal.Id,
            Property = "GOALS"
          },
        };

          if (updates != null && updates.AccountableUserChanged)
          {
            // User changed OR deleted
            await _eventSender.SendChangeAsync(ResourceNames.User(updates.OriginalAccountableUserId), Change<IMeetingChange>.Removed(Change.Target(goal.Id, GQL.Models.UserQueryModel.Collections.UserGoal.Goal), goal.Id, goal)).ConfigureAwait(false);
          }

          await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.RecurrenceId), Change<IMeetingChange>.Updated(goal.Id, goal, targets)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.RecurrenceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(goal.Id, GQL.Models.GoalQueryModel.Associations.User5.Assignee), goal.Assignee.Id, goal.Assignee)).ConfigureAwait(false);

          await _eventSender.SendChangeAsync(ResourceNames.Goal(goal.Id), Change<IMeetingChange>.Updated(goal.Id, goal, targets)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.Goal(goal.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(goal.Id, GQL.Models.GoalQueryModel.Associations.User5.Assignee), goal.Assignee.Id, goal.Assignee)).ConfigureAwait(false);

          await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.Updated(goal.Id, goal, targets)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.User(goal.Assignee.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(goal.Id, GQL.Models.UserQueryModel.Associations.UserGoals.Goals), goal.Id, goal)).ConfigureAwait(false);

        }
      }
    }   
  }
}
