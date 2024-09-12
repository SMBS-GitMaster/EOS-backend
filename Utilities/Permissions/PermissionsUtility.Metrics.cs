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
    public PermissionsUtility EditUserScorecard(long userId) {
      return EditUserOrganization(userId);
    }

    public PermissionsUtility ViewOrganizationScorecard(long organizationId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      if (IsManagingOrganization(organizationId)) {
        return this;
      }

      var organization = session.Get<OrganizationModel>(organizationId);
      if (organization.Settings.EmployeesCanViewScorecard && caller.Organization.Id == organizationId) {
        return this;
      }

      if (organization.Settings.ManagersCanViewScorecard && IsManager(organizationId)) {
        return this;
      }

      throw new PermissionsException();
    }

    public PermissionsUtility ViewScore(long scoreId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }
      var score = session.Get<ScoreModel>(scoreId);
      return ViewMeasurable(score.MeasurableId);
    }

    public bool IsMeasurableViewble(long measurableId)
    {
      try
      {
        ViewMeasurable(measurableId); return true;
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    public PermissionsUtility ViewMeasurable(long measurableId) {
      return CheckCacheFirst("ViewMeasurable", measurableId).Execute(() => {
        if (IsRadialAdmin(caller)) {
          return this;
        }

        var m = session.Get<MeasurableModel>(measurableId);
        if (IsManagingOrganization(m.OrganizationId)) {
          return this;
        }

        if (m.AccountableUserId == caller.Id) {
          return this;
        }

        if (m.AdminUserId == caller.Id) {
          return this;
        }

        try {
          ManagesUserOrganization(m.AccountableUserId, false);
          return this;
        } catch (PermissionsException) {
        }


        var measurableRecurs = session.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
            .Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
            .Select(x => x.L10Recurrence.Id)
            .List<long>().ToList();

        foreach (var recur in measurableRecurs) {
          try {
            ViewL10Recurrence(recur);
            return this;
          } catch (PermissionsException) {
          }
        }
        try {
          CanAdminMeetingWithUser(m.AccountableUserId, onError: new PermissionsException().Message);
          return this;
        } catch (PermissionsException) {
        }

        throw new PermissionsException("Cannot view measurable");
      });
    }
    public PermissionsUtility EditMeasurable(long measurableId) {
      return CheckCacheFirst("EditMeasurable", measurableId).Execute(() => {
        if (IsRadialAdmin(caller)) {
          return this;
        }

        ViewMeasurable(measurableId);
        var m = session.Get<MeasurableModel>(measurableId);


        if (m.AccountableUserId == caller.Id) {
          return this;
        }

        if (m.AdminUserId == caller.Id) {
          return this;
        }

        if (IsManagingOrganization(m.OrganizationId)) {
          return this;
        }

        var measurableRecurs = session.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
            .Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
            .Select(x => x.L10Recurrence.Id)
            .List<long>().ToList();

        foreach (var recur in measurableRecurs) {
          try {
            EditL10Recurrence(recur);
            return this;
          } catch (PermissionsException) {
          }
        }

        throw new PermissionsException();
      });
    }

    public PermissionsUtility EditScore(long scoreId) {
      var score = session.Get<ScoreModel>(scoreId);
      return EditMeasurable(score.MeasurableId);
    }

    public PermissionsUtility CanViewUserMeasurables(long userId) {
      return CanViewUserRocks(userId);
    }

    public PermissionsUtility CreateMeasurableForUser(long userId, long? recurrenceId = null) {
      return Or(x => x.EditUserDetails(userId), x => x.CanAdminMeetingWithUser(userId, recurrenceId, "Cannot create measurable"));
    }
  }
}
