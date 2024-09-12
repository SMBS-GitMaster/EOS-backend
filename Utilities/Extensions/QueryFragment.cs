using System;
using NHibernate;

using RadialReview.Models;
using RadialReview.GraphQL.Models;
using RadialReview.Core.GraphQL.Models;
using RadialReview.Utilities;
using RadialReview.Exceptions;

namespace RadialReview.Core.Utilities.Extensions;

public interface IPermissionModel
{
  MeetingPermissionsModel ResourcePermissions(PermItem.ResourceType resourceType, long resourceId);
  OrgChartPermissionsModel PermissionsForOrgChart(long orgChartId);
}

public static class QueryFragment
{
  public static IPermissionModel PermissionsForUser(this ISession session, long userOrganizationModelId)
  {
    var uom = session.Get<UserOrganizationModel>(userOrganizationModelId);
    return new PermissionModel(session, uom);
  }

  public static IPermissionModel PermissionsForUser(this ISession session, UserOrganizationModel user)
  {
    return new PermissionModel(session, user);
  }

  internal class PermissionModel : IPermissionModel
  {
    protected readonly PermissionsUtility userPerm;

    private static readonly MeetingPermissionsModel noaccess = new () {
      View = false,
      Edit = false,
      Admin = false,
    };

    internal PermissionModel(ISession session, UserOrganizationModel user)
    {
        userPerm = PermissionsUtility.Create(session, user);
    }

    public MeetingPermissionsModel ResourcePermissions(PermItem.ResourceType resourceType, long resourceId)
    {
      try
      {
        var perm = new MeetingPermissionsModel{
            View  = userPerm.IsPermitted(x => x.CanView(resourceType, resourceId)),
            Edit  = userPerm.IsPermitted(x => x.CanEdit(resourceType, resourceId)),
            Admin = userPerm.IsPermitted(x => x.CanAdmin(resourceType, resourceId)),
        };

        return perm;
      }
      catch(RedirectToActionException ex)
      {
        if (ex.Controller == "Payment" && ex.Action == "Lockout")
        {
          return noaccess;
        }

        throw ex;
      }
    }

    public OrgChartPermissionsModel PermissionsForOrgChart(long orgChartId)
    {
      var can = userPerm.IsPermitted(x => x.ViewHierarchy(orgChartId));
      return new OrgChartPermissionsModel {
        CanViewHierarchy = can,
      };
    }
  }
}