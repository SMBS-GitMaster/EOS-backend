using HotChocolate.Subscriptions;
using NHibernate;
using GQL = RadialReview.GraphQL;
using RadialReview.Core.Repositories;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Models;
using RadialReview.Models.Todo;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.GraphQL.Models;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_Todos : ITodoHook, IMeetingTodoHook
  {
    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_Todos(ITopicEventSender eventSender)
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

    public async Task AttachTodo(ISession s, UserOrganizationModel caller, TodoModel todo, bool ignoreNotification = false)
    {
      var response = SubscriptionResponse<long>.Added(todo.Id);

      await _eventSender.SendAsync(ResourceNames.TodoEvents, response);

      var t = todo.TransformTodo();
      var recurrenceId = t.ForRecurrenceId;
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Inserted(Change.Target(recurrenceId, GQL.Models.MeetingQueryModel.Collections.Todo.Todos), t.Id, t)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Inserted(Change.Target(recurrenceId, GQL.Models.MeetingQueryModel.Collections.Todo.TodosActives), t.Id, t)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(t.Assignee.Id, GQL.Models.TodoQueryModel.Associations.User2.Assignee), t.Assignee.Id, t.Assignee)).ConfigureAwait(false);

      await _eventSender.SendChangeAsync(ResourceNames.User(t.Assignee.Id), Change<IMeetingChange>.Inserted(Change.Target(recurrenceId, GQL.Models.MeetingQueryModel.Collections.Todo.Todos), t.Id, t)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(t.Assignee.Id), Change<IMeetingChange>.Inserted(Change.Target(recurrenceId, GQL.Models.MeetingQueryModel.Collections.Todo.TodosActives), t.Id, t)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(t.Assignee.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(t.Assignee.Id, GQL.Models.TodoQueryModel.Associations.User2.Assignee), t.Assignee.Id, t.Assignee)).ConfigureAwait(false);

      // User Todos relationship
      await _eventSender.SendChangeAsync(ResourceNames.User(t.Assignee.Id), Change<IMeetingChange>.Inserted(Change.Target(t.Assignee.Id, GQL.Models.UserQueryModel.Collections.UserNodeTodos.Todos), t.Id, t)).ConfigureAwait(false);
    }


    public async Task CreateTodo(ISession s, UserOrganizationModel caller, TodoModel todo)
    {
      var response = SubscriptionResponse<long>.Added(todo.Id);

      await _eventSender.SendAsync(ResourceNames.TodoEvents, response);

      var t = todo.TransformTodo();
      var recurrenceId = t.ForRecurrenceId;
      if (recurrenceId != 0)
      {
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Created(t.Id, t)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(t.Assignee.Id, GQL.Models.TodoQueryModel.Associations.User2.Assignee), t.Assignee.Id, t.Assignee)).ConfigureAwait(false);
      }

      await _eventSender.SendChangeAsync(ResourceNames.User(t.Assignee.Id), Change<IMeetingChange>.Created(t.Id, t)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(t.Assignee.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(t.Assignee.Id, GQL.Models.TodoQueryModel.Associations.User2.Assignee), t.Assignee.Id, t.Assignee)).ConfigureAwait(false);

      // User Todos relationship
      await _eventSender.SendChangeAsync(ResourceNames.User(t.Assignee.Id), Change<IMeetingChange>.Inserted(Change.Target(t.Assignee.Id, GQL.Models.UserQueryModel.Collections.UserNodeTodos.Todos), t.Id, t)).ConfigureAwait(false);
    }

    public async Task DetachTodo(ISession s, UserOrganizationModel caller, TodoModel todo)
    {
      var response = SubscriptionResponse<long>.Updated(todo.Id);

      await _eventSender.SendAsync(ResourceNames.TodoEvents, response);

      var t = todo.TransformTodo();
      var recurrenceId = t.ForRecurrenceId;
      if (recurrenceId != 0)
      {
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Removed(Change.Target(recurrenceId, GQL.Models.MeetingQueryModel.Collections.Todo.Todos), t.Id, t)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Removed(Change.Target(recurrenceId, GQL.Models.MeetingQueryModel.Collections.Todo.TodosActives), t.Id, t)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(t.Assignee.Id, GQL.Models.TodoQueryModel.Associations.User2.Assignee), t.Assignee.Id, t.Assignee)).ConfigureAwait(false);
      }

      await _eventSender.SendChangeAsync(ResourceNames.User(t.Assignee.Id), Change<IMeetingChange>.Removed(Change.Target(recurrenceId, GQL.Models.MeetingQueryModel.Collections.Todo.Todos), t.Id, t)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(t.Assignee.Id), Change<IMeetingChange>.Removed(Change.Target(recurrenceId, GQL.Models.MeetingQueryModel.Collections.Todo.TodosActives), t.Id, t)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(t.Assignee.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(t.Assignee.Id, GQL.Models.TodoQueryModel.Associations.User2.Assignee), t.Assignee.Id, t.Assignee)).ConfigureAwait(false);

      // User Todos relationship
      await _eventSender.SendChangeAsync(ResourceNames.User(t.Assignee.Id), Change<IMeetingChange>.Removed(Change.Target(t.Assignee.Id, GQL.Models.UserQueryModel.Collections.UserNodeTodos.Todos), t.Id, t)).ConfigureAwait(false);
    }


    public async Task UpdateTodo(ISession s, UserOrganizationModel caller, TodoModel todo, ITodoHookUpdates updates)
    {
      var response = SubscriptionResponse<long>.Updated(todo.Id);

      await _eventSender.SendAsync(ResourceNames.TodoEvents, response);
      await SendUpdatedEventOnMeetingChannel(todo, updates);
    }

    private async Task SendUpdatedEventOnMeetingChannel(TodoModel todo, ITodoHookUpdates updates)
    {
      var t = todo.TransformTodo();
      var meetingId = t.ForRecurrenceId;

      ContainerTarget[] targets;

      if (updates.AccountableUserChanged)
      {
        targets = new[]{
          new ContainerTarget {
            Type = "meeting",
            Id = meetingId,
            Property = "TODOS",
          },

          new ContainerTarget {
            Type = "meeting",
            Id = meetingId,
            Property = "TODOS_ACTIVES",
          },
          new ContainerTarget {
            Type = "user",
            Id = todo.AccountableUserId,
            Property = "TODOS",
          },

          new ContainerTarget {
            Type = "user",
            Id = updates.PreviousAccountableUser,
            Property = "TODOS",
          },
        };
      }
      else
      {
          targets = new[]{
          new ContainerTarget {
            Type = "user",
            Id = todo.AccountableUserId,
            Property = "TODOS",
          },

          new ContainerTarget {
            Type = "meeting",
            Id = meetingId,
            Property = "TODOS",
          },

          new ContainerTarget {
            Type = "meeting",
            Id = meetingId,
            Property = "TODOS_ACTIVES",
          },
        };
      }


      await _eventSender.SendChangeAsync(ResourceNames.Meeting(meetingId), Change<IMeetingChange>.Updated(t.Id, t, targets)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Todo(t.Id), Change<IMeetingChange>.Updated(t.Id, t, targets)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(todo.AccountableUserId), Change<IMeetingChange>.Updated(t.Id, t, targets)).ConfigureAwait(false);

      if (updates.AccountableUserChanged)
      {
        var assignee = GQL.Models.TodoQueryModel.Associations.User2.Assignee;
        var change = Change<IMeetingChange>.UpdatedAssociation(Change.Target(t.Assignee.Id, assignee), t.Assignee.Id, t.Assignee);

        await _eventSender.SendChangeAsync(ResourceNames.Meeting(meetingId), change).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Todo(t.Id), change).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.User(todo.AccountableUserId), change).ConfigureAwait(false);

        await _eventSender.SendChangeAsync(ResourceNames.User(updates.PreviousAccountableUser), Change<IMeetingChange>.Removed(Change.Target(updates.PreviousAccountableUser, UserQueryModel.Collections.UserNodeTodos.Todos), t.Id, t)).ConfigureAwait(false);
      }

      if (updates.PreviousRecurrenceId.HasValue)
      {
        var change = Change<IMeetingChange>.Removed(Change.Target(updates.PreviousRecurrenceId.Value, GQL.Models.MeetingQueryModel.Collections.Todo.Todos), t.Id, t);
        var change2 = Change<IMeetingChange>.Removed(Change.Target(updates.PreviousRecurrenceId.Value, GQL.Models.MeetingQueryModel.Collections.Todo.TodosActives), t.Id, t);

        await _eventSender.SendChangeAsync(ResourceNames.Meeting(updates.PreviousRecurrenceId.Value), change).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(updates.PreviousRecurrenceId.Value), change2).ConfigureAwait(false);

        if (todo.ForRecurrence != null)
        {
          await _eventSender.SendChangeAsync(ResourceNames.Meeting(todo.ForRecurrence.Id), Change<IMeetingChange>
            .Inserted(Change.Target(todo.ForRecurrence.Id, GQL.Models.MeetingQueryModel.Collections.Todo.Todos), t.Id, t)).ConfigureAwait(false);

          await _eventSender.SendChangeAsync(ResourceNames.Meeting(todo.ForRecurrence.Id), Change<IMeetingChange>
            .Inserted(Change.Target(todo.ForRecurrence.Id, GQL.Models.MeetingQueryModel.Collections.Todo.TodosActives), t.Id, t)).ConfigureAwait(false);

          //Getting MeetingQueryModel to sent in Todo Channel
          FavoriteModel favorite = FavoriteAccessor.GetFavoriteForUser(todo.AccountableUser, FavoriteType.Meeting, todo.ForRecurrence.Id);
          MeetingSettingsModel settings = MeetingSettingsAccessor.GetSettingsForMeeting(todo.AccountableUser, todo.ForRecurrence.Id);
          MeetingQueryModel meeting = todo.ForRecurrence.MeetingFromRecurrence(todo.AccountableUser, favorite, settings);
          var changeAssociation = Change<IMeetingChange>.UpdatedAssociation(Change.Target(meeting.Id, GQL.Models.TodoQueryModel.Associations.Meeting5.Meeting), meeting.Id, meeting);
          await _eventSender.SendChangeAsync(ResourceNames.Todo(t.Id), changeAssociation).ConfigureAwait(false);
        }

      }
    }
  }
}
