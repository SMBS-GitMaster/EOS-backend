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

    public PermissionsUtility CreateVTO(long organizationId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      if (IsManager(organizationId)) {
        return this;
      }

      throw new PermissionsException("Cannot create a VTO");
    }
    public PermissionsUtility ViewVTOVision(long vtoId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var vto = session.Get<VtoModel>(vtoId);
      if (vto.L10Recurrence.HasValue && vto.L10Recurrence.Value > 0) {
        var l10 = session.Get<L10Recurrence>(vto.L10Recurrence.Value);
        if (l10.ShareVto && l10.Organization.Settings.ShareVtoPages.ViewVision()) {
          return Or(() => ViewOrganization(l10.OrganizationId).ViewOrganization(vto.Organization.Id), () => ViewL10Recurrence(vto.L10Recurrence.Value));
        }

        return ViewL10Recurrence(vto.L10Recurrence.Value);
      } else {
        return CanView(PermItem.ResourceType.VTO, vtoId, @this => {
          if (IsManagingOrganization(vto.Organization.Id)) {
            return this;
          }

          if (vto.L10Recurrence != null) {
            return @this.ViewL10Recurrence(vto.L10Recurrence.Value);
          }

          throw new PermissionsException("Cannot view Business Plan");
        });
      }
    }

    public bool IsVtoTractionViewable(long vtoId)
    {
      try
      {
        ViewVTOTraction(vtoId);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public PermissionsUtility ViewVTOTraction(long vtoId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var vto = session.Get<VtoModel>(vtoId);
      if (vto.L10Recurrence.HasValue && vto.L10Recurrence.Value > 0) {
        var l10 = session.Get<L10Recurrence>(vto.L10Recurrence.Value);
        if (l10.ShareVto && l10.Organization.Settings.ShareVtoPages.ViewTraction()) {
          return Or(() => ViewOrganization(l10.OrganizationId).ViewOrganization(vto.Organization.Id), () => ViewL10Recurrence(vto.L10Recurrence.Value));
        }
        return ViewL10Recurrence(vto.L10Recurrence.Value);
      } else {
        return CanView(PermItem.ResourceType.VTO, vtoId, @this => {
          if (IsManagingOrganization(vto.Organization.Id)) {
            return this;
          }

          if (vto.L10Recurrence != null) {
            return @this.ViewL10Recurrence(vto.L10Recurrence.Value);
          }

          throw new PermissionsException("Cannot view Business Plan");
        });
      }
    }

    public PermissionsUtility ViewVTOTractionIssues(long vtoId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var vto = session.Get<VtoModel>(vtoId);
      if (vto.L10Recurrence.HasValue && vto.L10Recurrence.Value > 0) {
        var l10 = session.Get<L10Recurrence>(vto.L10Recurrence.Value);
        if (l10.ShareVto && l10.Organization.Settings.ShareVtoPages.IncludeIssues()) {
          return Or(() => ViewOrganization(l10.OrganizationId).ViewOrganization(vto.Organization.Id), () => ViewL10Recurrence(vto.L10Recurrence.Value));
        }
        return ViewL10Recurrence(vto.L10Recurrence.Value);
      } else {
        return CanView(PermItem.ResourceType.VTO, vtoId, @this => {
          if (IsManagingOrganization(vto.Organization.Id)) {
            return this;
          }

          if (vto.L10Recurrence != null) {
            return @this.ViewL10Recurrence(vto.L10Recurrence.Value);
          }

          throw new PermissionsException("Cannot view Business Plan");
        });
      }
    }



    public PermissionsUtility EditVTO(long vtoId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      return CanEdit(PermItem.ResourceType.VTO, vtoId, @this => {
        var vto = session.Get<VtoModel>(vtoId);
        if (vto.L10Recurrence != null) {
          return @this.EditL10Recurrence(vto.L10Recurrence.Value);
        } else {
          if (IsManagingOrganization(vto.Organization.Id)) {
            return this;
          }
        }
        throw new PermissionsException("Cannot edit Business Plan");
      });


    }
  }
}
