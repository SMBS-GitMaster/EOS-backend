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
    public PermissionsUtility ViewTeam(long teamId) {

      if (IsRadialAdmin(caller)) {
        return this;
      }

      var team = session.Get<OrganizationTeamModel>(teamId);


      if (team == null) {
        throw new PermissionsException();
      }

      if (!team.Secret && team.Organization.Id == caller.Organization.Id) {
        return this;
      }

      if (team.Secret && (team.CreatedBy == caller.Id || team.ManagedBy == caller.Id)) {
        return this;
      }

      var members = session.QueryOver<TeamDurationModel>().Where(x => x.TeamId == teamId && x.UserId == caller.Id).List().ToList();
      if (team.Secret && members.Any()) {
        return this;
      }



      throw new PermissionsException();
    }


    public PermissionsUtility IssueForTeam(long forTeamId) {
      return TryWithOverrides(p => {
        return ManagingTeam(forTeamId);
      });
    }

    public PermissionsUtility ManagingTeam(long teamId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var team = session.Get<OrganizationTeamModel>(teamId);

      if (IsManagingOrganization(team.Organization.Id, true)) {
        return this;
      }

      if (team.OnlyManagersEdit && team.ManagedBy == caller.Id) {
        return this;
      }

      var members = session.QueryOver<TeamDurationModel>().Where(x => x.Team.Id == teamId).List().ToListAlive();

      if (!team.OnlyManagersEdit && members.Any(x => x.User.Id == caller.Id)) {
        return this;
      }

      throw new PermissionsException();
    }



    public PermissionsUtility EditTeam(long teamId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      //Creating
      if (teamId == 0 && caller.IsManager()) {
        return this;
      }


      var team = session.Get<OrganizationTeamModel>(teamId);
      if (IsManagingOrganization(team.Organization.Id, true)) {
        return this;
      }

      if (team.Type != TeamType.Standard) {
        throw new PermissionsException("Cannot edit auto-populated team.");
      }

      if (caller.IsManager() || !team.OnlyManagersEdit) {
        if (team.Organization.Id == caller.Organization.Id) {
          if (!team.Secret) {
            return this;
          }

          if (team.Secret && (team.CreatedBy == caller.Id || team.ManagedBy == caller.Id)) {
            return this;
          }

          if (!team.OnlyManagersEdit) {
            var members = session.QueryOver<TeamDurationModel>().Where(x => x.TeamId == teamId && x.UserId == caller.Id).List().ToList();
            if (team.Secret && members.Any()) {
              return this;
            }
          }
        }
      }

      throw new PermissionsException();
    }
  }
}
