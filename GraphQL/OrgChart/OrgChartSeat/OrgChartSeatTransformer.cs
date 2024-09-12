using RadialReview.Models.Angular.Accountability;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Positions;
using RadialReview.GraphQL.Models;
using Microsoft.IdentityModel.Tokens;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Askables;

namespace RadialReview.Core.GraphQL.OrgChart.OrgChartSeat
{
  public static class OrgChartSeatTransformer
  {
    public static AngularAccountabilityNode ToAngularAccountabilityNode(this OrgChartSeatEditModel orgChartSeatEditModel, AccountabilityNode node)
    {
      var angularModel = new AngularAccountabilityNode()
      {
        Id = orgChartSeatEditModel.SeatId
      };

      var groups = node.GetAccountabilityRolesGroup();
      var roles = node.GetRoles();

      // update title seat
      if(orgChartSeatEditModel.PositionTitle != null)
      {
        angularModel.Group = new AngularAccountabilityGroup()
        {
          Id = groups.Id,
          Position =  new AngularPosition()
          {
            Id = groups.Id,
            Name = orgChartSeatEditModel.PositionTitle,
          }
        };
      }

      // update userid relation
      if (orgChartSeatEditModel.UserIds != null)
      {
        angularModel.Users = orgChartSeatEditModel.UserIds.Select(userId => new AngularUser() { Id = userId });
      }

      return angularModel;
    }

    public static OrgChartPositionQueryModel ToPositionQueryModel(this AccountabilityNode node)
    {
      var groups = node.GetAccountabilityRolesGroup();
      var roles = node.GetRoles();

      var source  = new OrgChartPositionQueryModel()
      {
        Id = node.AccountabilityRolesGroupId,
        Roles = roles.Select(r => r.ToPositionRoleQueryModel()).ToArray(),
        Title = groups.PositionName
      };

      return source;
    }
    public static OrgChartPositionRoleQueryModel ToPositionRoleQueryModel(this SimpleRole node)
    {
      var source  = new OrgChartPositionRoleQueryModel()
      {
        Id = node.Id,
        Name = node.Name
      };

      return source;
    }

    public static OrgChartSeatQueryModel ToOrgChartSeatQueryModel(this AccountabilityNode node)
    {
      return new OrgChartSeatQueryModel()
      {
        Id = node.Id
      };
    }
    public static OrgChartPositionRoleQueryModel ToOrgChartSeatRoleQueryModel(this SimpleRole role)
    {
      return new OrgChartPositionRoleQueryModel()
      {
        Id = role.Id,
        Name = role.Name
      };
    }

    public static AngularRole ToAngularRole(this OrgChartSeatEditRoleModel orgChartSeatEditRoleModel)
    {
      return new AngularRole()
      {
        Id = orgChartSeatEditRoleModel.Id,
        Name = orgChartSeatEditRoleModel.Name,
      };
    }
  }
}
