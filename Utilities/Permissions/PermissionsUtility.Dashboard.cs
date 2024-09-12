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


    public PermissionsUtility ViewDashboardForUser(String userid) {
      var user = session.Get<UserModel>(userid);
      if (user == null) {
        throw new PermissionsException("Workspace not found");
      }

      if (IsRadialAdmin(caller)) {
        return this;
      }

      if (userid == caller.User.Id) {
        return this;
      }

      throw new PermissionsException("Cannot view workspace");
    }

    public PermissionsUtility ViewDashboard(DashboardType modelType, long modelId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      switch (modelType) {
        case DashboardType.Standard:
          return ViewDashboard_Standard(modelId);
        case DashboardType.L10:
          try {
            return ViewL10Recurrence(modelId);
          } catch (PermissionsException) {
            throw new PermissionsException("Cannot view workspace.");
          }

        default:
          throw new PermissionsException("Cannot view workspace");
      }
    }

    private PermissionsUtility ViewDashboard_Standard(long dashboardId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var dash = session.Get<Dashboard>(dashboardId);
      if (dash == null) {
        throw new PermissionsException("Workspace not found");
      }

      return ViewDashboardForUser(dash.ForUser.Id);
    }

    public PermissionsUtility EditDashboard(DashboardType type, long dashboardId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      if (type == DashboardType.Standard) {
        var dash = session.Get<Dashboard>(dashboardId);
        if (dash == null) {
          throw new PermissionsException("Workspace not found");
        }

        if (dash.ForUser.Id == caller.User.Id) {
          return this;
        }
      }

      throw new PermissionsException("Cannot edit workspace");

    }

    public PermissionsUtility EditTile(long tileId) {
      var tile = session.Get<TileModel>(tileId);
      if (tile == null) {
        throw new PermissionsException("Tile not found");
      }

      if (IsRadialAdmin(caller)) {
        return this;
      }

      if (tile.ForUser.Id == caller.User.Id) {
        return this;
      }

      throw new PermissionsException("Cannot edit tile");
    }

  }
}
