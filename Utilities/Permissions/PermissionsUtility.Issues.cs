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
    public PermissionsUtility EditIssueRecurrence(long issueRecurrenceId) {
      var issueRecurrence = session.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);
      return EditIssue(issueRecurrence.Issue.Id);
    }

    public PermissionsUtility EditIssue(long issueId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var possibleRecurrences = session.QueryOver<IssueModel.IssueModel_Recurrence>()
        .Where(x => x.DeleteTime == null && x.Issue.Id == issueId)
        .Select(x => x.Recurrence.Id).List<long>()
        .ToList();

      foreach (var p in possibleRecurrences) {
        try {
          return EditL10Recurrence(p);
        } catch (PermissionsException) {
          //try next one..
        }
      }
      throw new PermissionsException();
    }
    public PermissionsUtility ViewIssueRecurrence(long issueRecurrenceId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var issueRecurrence = session.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);
      return ViewIssue(issueRecurrence.Issue.Id);
    }

    public PermissionsUtility ViewIssue(long issueId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var possibleRecurrences = session.QueryOver<IssueModel.IssueModel_Recurrence>()
        .Where(x => x.DeleteTime == null && x.Issue.Id == issueId)
        .Select(x => x.Recurrence.Id).List<long>()
        .ToList();

      foreach (var p in possibleRecurrences) {
        try {
          return ViewL10Recurrence(p);
        } catch (PermissionsException) {
          //try next one..
        }
      }
      throw new PermissionsException();
    }
  }
}
