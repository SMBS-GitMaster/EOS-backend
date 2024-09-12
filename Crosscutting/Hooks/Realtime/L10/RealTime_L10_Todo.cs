using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using NHibernate;
using RadialReview.Models.Todo;
using System.Threading.Tasks;
using RadialReview.Hubs;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Utilities.RealTime;
using RadialReview.Core.Models.Terms;
using RadialReview.Core.Accessors;
using RadialReview.Utilities;

namespace RadialReview.Crosscutting.Hooks.Realtime.L10 {
  public class RealTime_L10_Todo : ITodoHook, IMeetingTodoHook {
    public bool AbsorbErrors() {
      return false;
    }
    public bool CanRunRemotely() {
      return false;
    }

    public HookPriority GetHookPriority() {
      return HookPriority.UI;
    }
    public async Task CreateTodo(ISession s, UserOrganizationModel caller, TodoModel todo) {

      if (todo.TodoType == TodoType.Personal) {
        //Need to be duplicated.. one filters out the caller
        await using (var rt = RealTimeUtility.Create(/*NO CALLER FILTER*/)) {
          var todoData = TodoData.FromTodo(todo);
          rt.UpdateGroup(RealTimeHub.Keys.UserId(todo.AccountableUserId)).Call("appendTodo", ".todo-list", todoData);
        }

        await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString()/*CALLER FILTER*/)) {
          var group = rt.UpdateGroup(RealTimeHub.Keys.UserId(todo.AccountableUserId));
          group.Update(new ListDataVM(todo.AccountableUserId) {
            Todos = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularTodo(todo))
          });
        }
      }
    }

    public async Task UpdateTodo(ISession s, UserOrganizationModel caller, TodoModel todo, ITodoHookUpdates updates) {


      await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
        List<RealTimeUtility.GroupUpdater> groups = new List<RealTimeUtility.GroupUpdater>();
        if (todo.TodoType == TodoType.Recurrence) {
          groups.Add(rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(todo.ForRecurrenceId.Value)));
        }

        groups.Add(rt.UpdateGroup(RealTimeHub.Keys.UserId(todo.AccountableUserId)));

        var updatedTodo = new TodoModel() {
          Id = todo.Id,
          CreateTime = todo.CreateTime,
          CreatedDuringMeeting = new Models.L10.L10Meeting() {
            StartTime = todo.CreatedDuringMeeting?.StartTime
          },
          DueDate = todo.DueDate,
          CompleteTime = todo.CompleteTime
        };

        bool IsTodoUpdate = false;
        if (updates.MessageChanged) {
          groups.ForEach(g => g.Call("updateTodoMessage", todo.Id, todo.Message));
        }
        if (updates.DueDateChanged) {
          groups.ForEach(g => g.Call("updateTodoDueDate", updatedTodo, todo.DueDate));
        }
        if (updates.AccountableUserChanged) {
          groups.ForEach(g => g.Call("updateTodoAccountableUser", todo.Id, todo.AccountableUserId, todo.AccountableUser.GetName(), todo.AccountableUser.ImageUrl(true, ImageSize._32)));
        }

        if (updates.CompletionChanged) {
          if (todo.CompleteTime != null) {
          } else if (todo.CompleteTime == null) {
          }

          groups.ForEach(g => g.Call("updateTodoCompletion", updatedTodo));

          //Re-add
          if (todo.CompleteTime == null && todo.ForRecurrenceId > 0) {
            groups.ForEach(g => g.Update(new AngularRecurrence(todo.ForRecurrenceId.Value) {
              Todos = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularTodo(todo) {
                CompleteTime = Removed.Date()
              })
            }));
            var userGroup = rt.UpdateGroup(RealTimeHub.Keys.UserId(todo.AccountableUserId));
            userGroup.Update(new ListDataVM(todo.AccountableUserId) {
              Todos = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularTodo(todo) {
                CompleteTime = Removed.Date()
              })
            });
          }
        }


        groups.ForEach(g => g.Update(new AngularTodo(todo)));
      }
    }

    public async Task AttachTodo(ISession s, UserOrganizationModel caller, TodoModel todo, bool ignoreNotification = false) {
      await using (var rt = RealTimeUtility.Create()) {
        if (todo.TodoType == TodoType.Personal) {
          var userMeetingHub = rt.UpdateGroup(RealTimeHub.Keys.UserId(todo.AccountableUserId));
          var todoData = TodoData.FromTodo(todo);
          userMeetingHub.Call("appendTodo", ".todo-list", todoData);
        } else {
          var recurrenceId = todo.ForRecurrenceId.Value;
          var meetingHub = rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));
          var todoData = TodoData.FromTodo(todo);

          if (todo.CreatedDuringMeetingId != null)
            todoData.isNew = true;
          meetingHub.Call("appendTodo", ".todo-list", todoData);

          if (!ignoreNotification) {


            TermsCollection terms = TermsCollection.DEFAULT;
            try {
              terms= TermsAccessor.GetTermsCollection(s, PermissionsUtility.Create(s, caller), caller.Organization.Id);
            } catch (Exception e) {

            }

            var message = $@"Created {terms.GetTermSingular(TermKey.ToDos)}.";
            try {
              message = todo.CreatedBy.GetFirstName() + $@" created {terms.GetTermSingular(TermKey.ToDos)}.";
            } catch (Exception) { }
            meetingHub.Call("showAlert", message, null, null, 3000);
          }

          var updates = new AngularRecurrence(recurrenceId);
          updates.Todos = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularTodo(todo));
          meetingHub.Update(updates);

          if (RealTimeHelpers.GetConnectionString() != null) {
            var me = rt.UpdateConnection(RealTimeHelpers.GetConnectionString());
            me.Update(new AngularRecurrence(recurrenceId) {
              Focus = "[data-todo='" + todo.Id + "'] input:visible:first"
            });
          }
        }
      }
      if (todo.TodoType == TodoType.Personal) {
        //Separate because this one filters out the caller.
        await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString()/*CALLER FILTER*/)) {
          var group = rt.UpdateGroup(RealTimeHub.Keys.UserId(todo.AccountableUserId));
          group.Update(new ListDataVM(todo.AccountableUserId) {
            Todos = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularTodo(todo))
          });
        }
      }
    }

    public async Task DetachTodo(ISession s, UserOrganizationModel caller, TodoModel todo) {
      await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {

        var groups = new List<RealTimeUtility.GroupUpdater>();
        if (todo.TodoType == TodoType.Recurrence) {
          groups.Add(rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(todo.ForRecurrenceId.Value)));
        }

        groups.Add(rt.UpdateGroup(RealTimeHub.Keys.UserId(todo.AccountableUserId)));
        if (todo.ForRecurrenceId != null) {
          groups.ForEach(g => g.Update(new AngularRecurrence(todo.ForRecurrenceId.Value) {
            Todos = AngularList.CreateFrom(AngularListType.Remove, new AngularTodo(todo.Id))
          }));
        } else {
          groups.ForEach(g => g.Update(new ListDataVM(todo.AccountableUserId) {
            Todos =AngularList.CreateFrom(AngularListType.Remove, new AngularTodo(todo.Id))
          }));
        }
      }
    }
  }
}
