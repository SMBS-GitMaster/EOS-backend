using log4net;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.EventAnalyzers.Models;
using RadialReview.Crosscutting.Zapier;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Admin;
using RadialReview.Models.Askables;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Documents;
using RadialReview.Models.Enums;
using RadialReview.Models.Integrations;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Permissions;
using RadialReview.Models.Prereview;
using RadialReview.Models.Process;
using RadialReview.Models.Rocks;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Survey;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Models.UserTemplate;
using RadialReview.Models.VTO;
using RadialReview.Reflection;
using RadialReview.Utilities.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace RadialReview.Utilities {
  public partial class PermissionsUtility {
    public PermissionsUtility AssignTodo(long userId, long? recurrenceId) {
      if (userId <= 0) {
        throw new PermissionsException("Invalid UserId");
      }

      if (IsRadialAdmin(caller)) {
        return this;
      }

      if (recurrenceId == null) {
        if (userId == caller.Id) {
          return this;
        }

        try {
          return ManagesUserOrganization(userId, false);
        } catch (Exception) {
        }
      }

      ViewUserOrganization(userId, false);
      if (recurrenceId != null) {
        return CanEdit(PermItem.ResourceType.L10Recurrence, recurrenceId.Value, includeAlternateUsers: true);
      }
      throw new PermissionsException("Cannot assign this user a to-do");
    }


    public PermissionsUtility EditTodo(long todoId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var todo = session.Get<TodoModel>(todoId);

      if (IsSelf(todo.AccountableUserId)) {
        return this;
      }

      if (todo.ForRecurrenceId != null && todo.ForRecurrenceId != 0) {
        try {
          EditL10Recurrence(todo.ForRecurrenceId.Value);
          return this;
        } catch (PermissionsException) {
        }
      } else {
        if (IsManagingOrganization(todo.OrganizationId)) {
          return this;
        }
      }
      throw new PermissionsException();
    }

    public PermissionsUtility ViewTodo(long todoId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var todo = session.Get<TodoModel>(todoId);

      if (IsSelf(todo.AccountableUserId)) {
        return this;
      }

      if (todo.ForRecurrenceId != null && todo.ForRecurrenceId != 0) {
        try {
          ViewL10Recurrence(todo.ForRecurrenceId.Value);
          return this;
        } catch (PermissionsException) {
        }
      } else {
        if (IsManagingOrganization(todo.OrganizationId)) {
          return this;
        }
      }
      throw new PermissionsException();
    }

  }
}
