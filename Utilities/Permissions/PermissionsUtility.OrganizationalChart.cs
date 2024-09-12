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
    public PermissionsUtility ViewAccountabilityNode(long nodeId) {
      var node = session.Get<AccountabilityNode>(nodeId);
      return ViewHierarchy(node.AccountabilityChartId);
    }

    public PermissionsUtility ViewHierarchy(long hierarchyId) {
      return CanView(PermItem.ResourceType.AccountabilityHierarchy, hierarchyId, x => {
        var chart = session.Get<AccountabilityChart>(hierarchyId);
        x.ViewOrganization(chart.OrganizationId);
        return x;
      });
    }

    public PermissionsUtility ManagesAccountabilityNodeOrSelf(long nodeId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var node = session.Get<AccountabilityNode>(nodeId);
      try {
        return TryWithOverrides(x => {

          if (IsManagingOrganization(node.OrganizationId, true)) {
            return x;
          }

          try {
            return EditHierarchy(node.AccountabilityChartId);
          } catch (PermissionsException) {

            var users = node.GetUsers(session);
            if (users.Any() && users.All(u => IsPermitted(p => p.EditUserOrganization(u.Id)))) {
              return x;
            }

            if (DeepAccessor.Permissions.ManagesNode(session, this, caller.Id, nodeId)) {
              return x;
            }
          }
          throw new PermissionsException("You do not manage this seat.");
        });
      } catch (PermissionsException) {
        throw new PermissionsException("You do not manage this seat.") {
        };
      }

    }

    public PermissionsUtility EditHierarchy(long hierarchyId) {
      return CanEdit(PermItem.ResourceType.AccountabilityHierarchy, hierarchyId, x => {

        var chart = session.Get<AccountabilityChart>(hierarchyId);
        ViewOrganization(chart.OrganizationId);

        //Both are managers at the organization
        if (!(caller.ManagerAtOrganization || caller.ManagingOrganization)) {
          throw new PermissionsException();
        }

        return x;
      });
    }

    public PermissionsUtility ViewSimpleRole(long simpleRoleId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var role = session.Get<SimpleRole>(simpleRoleId);
      return ViewOrganization(role.OrgId);
    }

    public PermissionsUtility EditSimpleRole(long simpleRoleId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }
      var role = session.Get<SimpleRole>(simpleRoleId);
      var node = session.Get<AccountabilityNode>(role.NodeId);
      try {
        return EditHierarchy(node.AccountabilityChartId);
      } catch (PermissionsException) {
        //required to populate users.
        var _ = node.GetUsers(session);
        if (caller.Organization.Settings.EmployeesCanEditSelf && node.ContainsOnlyUser(caller.Id)) {
          return this;
        }
        if (caller.IsManager() && caller.Organization.Settings.ManagersCanEditSelf && node.ContainsOnlyUser(caller.Id)) {
          return this;
        }

        if (node.UserCount() > 1) {
          throw new PermissionsException("Cannot edit roles for other users.");
        }
        throw new PermissionsException("Cannot edit role");
      }
    }


    [Obsolete("use EditSimpleRole instead")]
    public PermissionsUtility EditRole_Deprecated(long roleId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var role = session.Get<RoleModel_Deprecated>(roleId);
      if (IsManagingOrganization(role.OrganizationId)) {
        return this;
      }

      var ordering = new[] { AttachType.User, AttachType.Position, AttachType.Team }.ToList();
      var links = session.QueryOver<RoleLink_Deprecated>()
        .Where(x => x.DeleteTime == null && x.RoleId == roleId)
        .List()
        .OrderBy(x => ordering.IndexOf(x.AttachType))
        .ToList();

      try {
        var ors = links.Select(x => new Func<PermissionsUtility>(() => EditAttach_Deprecated(x.GetAttach()))).ToList();
        ors.Add(() => {
          var org = session.Get<OrganizationModel>(role.OrganizationId);
          return EditHierarchy(org.AccountabilityChartId);
        });
        return Or(ors.ToArray());
      } catch (Exception) {
        throw new PermissionsException("Cannot edit role.") {
          NoErrorReport = true
        };
      }
    }


    [Obsolete("Deprecated")]
    public PermissionsUtility ManagingPosition_Deprecated(long positionId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      if (positionId == 0) {
        return this;
      }

      var position = session.Get<Deprecated.OrganizationPositionModel>(positionId);

      if (IsManagingOrganization(position.Organization.Id, true)) {
        return this;
      }

      if (caller.Organization.ManagersCanEditPositions && caller.ManagerAtOrganization && position.Organization.Id == caller.Organization.Id) {
        return this;
      }

      throw new PermissionsException("You are not permitted to manage functions.");
    }

    public PermissionsUtility EditPositions(long organizationId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var org = session.Get<OrganizationModel>(organizationId);
      try {
        return EditHierarchy(org.AccountabilityChartId);
      } catch (PermissionsException e) {
      }

      if (IsManagingOrganization(organizationId, org.ManagersCanEditPositions)) {
        return this;
      }
      if (caller.Organization.ManagersCanEditPositions && caller.ManagerAtOrganization) {
        return this;
      }

      throw new PermissionsException("You are not permitted to edit functions.");
    }



    [Obsolete("broken", true)]
    public PermissionsUtility EditAttach_Deprecated(Attach attachTo) {

      if (IsRadialAdmin(caller)) {
        return this;
      }

      var orgId = AttachAccessor.GetOrganizationId_Deprecated(session, attachTo);
      ViewOrganization(orgId);

      if (IsManagingOrganization(orgId)) {
        return this;
      }

      switch (attachTo.Type) {
        case AttachType.Position:
          return EditPositions(orgId);
        case AttachType.Team:
          return EditTeam(attachTo.Id);
        case AttachType.User:
          return EditUserOrganization(attachTo.Id);
        default:
          throw new PermissionsException("Invalid attach type (" + attachTo.Type + ")");
      }
    }
  }
}
