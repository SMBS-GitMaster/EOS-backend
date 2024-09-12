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
    public PermissionsUtility Self(IForModel forModel) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      if (forModel.ModelType == ForModel.GetModelType<UserOrganizationModel>()) {
        return Self(forModel.ModelId);
      }

      throw new PermissionsException();
    }

    public PermissionsUtility Self(long userId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      if (userId == caller.Id) {
        return this;
      }

      if (caller.UserIds != null && caller.UserIds.Any(x => x == userId)) {
        return this;
      }

      throw new PermissionsException();
    }

    protected bool IsSelf(long id_DONT_USE_CALLER_ID) {

      if (id_DONT_USE_CALLER_ID == caller.Id) {
        return true;
      }

      if (caller.User != null && caller.User.UserOrganizationIds != null && caller.User.UserOrganizationIds.Contains(id_DONT_USE_CALLER_ID)) {
        var found = session.Get<UserOrganizationModel>(id_DONT_USE_CALLER_ID);
        if (found.DeleteTime != null) {
          return false;
        }

        return true;
      }
      return false;

    }
  }
}
