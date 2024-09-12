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

    public PermissionsUtility EditResponsibility(long responsibilityId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var r = session.Get<ResponsibilityModel>(responsibilityId);
      var rGroupId = r.ForResponsibilityGroup;
      ResponsibilityGroupModel rGroup = session.Get<ResponsibilityGroupModel>(rGroupId);

      if (rGroup is OrganizationModel) {
        return EditOrganization(rGroupId);
      } else if (rGroup is OrganizationTeamModel) {
        return EditTeam(rGroupId);
      } else if (rGroup is UserOrganizationModel) {
        return EditUserOrganization(rGroupId);
      } else {
        throw new PermissionsException("Unknown responsibility group type.");
      }
    }

    public PermissionsUtility ViewRGM(long id) {
      var rgm = session.Get<ResponsibilityGroupModel>(id);
      rgm = (ResponsibilityGroupModel)session.GetSessionImplementation().PersistenceContext.Unproxy(rgm);

      if (rgm is OrganizationModel) {
        return ViewOrganization(rgm.Id);
      } else if (rgm is OrganizationTeamModel) {
        return ViewTeam(rgm.Id);
      } else if (rgm is UserOrganizationModel) {
        return ViewUserOrganization(rgm.Id, false);
      }

      throw new PermissionsException("Unknown responsibility group type.");
    }

  }
}
