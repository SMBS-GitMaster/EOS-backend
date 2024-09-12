namespace RadialReview.Repositories;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using RadialReview.Core.GraphQL.Models;
using RadialReview.Utilities;
using RadialReview.GraphQL.Models;
using System.Threading.Tasks;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.OrgChart.OrgChartSeat;
using RadialReview.Accessors;
using RadialReview.Models.Accountability;
using DocumentFormat.OpenXml.Office2010.Excel;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Positions;
using RadialReview.Core.Repositories;

public partial interface IRadialReviewRepository
{
  IQueryable<(long OrgChartId, OrgChartSeatQueryModel OrgChartSeat)> GetOrgChartSeatsForOrgCharts(IEnumerable<long> orgChartIds, CancellationToken cancellationToken);
  IQueryable<(long OrgChartSeatId, UserQueryModel User)> GetUsersForOrgChartSeats(IEnumerable<long> orgChartSeatIds, CancellationToken cancellationToken);
  IQueryable<(long OrgChartSeatId, OrgChartPositionQueryModel Position)> GetPositionsForOrgChartSeats(IEnumerable<long> orgChartSeatIds, CancellationToken cancellationToken);
  Task<List<OrgChartPositionRoleQueryModel>> GetPositionRoleForOrgChartSeat(long orgChartSeatId, CancellationToken cancellationToken);

  IQueryable<(long OrgChartSeatId, OrgChartSeatQueryModel OrgChartSeat)> GetDirectReportsForOrgChartSeats(IEnumerable<long> orgChartSeatIds, CancellationToken cancellationToken);

  #region Mutations
  Task<GraphQLResponseBase> EditOrgChartSeat(OrgChartSeatEditModel orgChartSeatEditModel);

  Task<GraphQLResponseBase> EditRoleChartSeat(OrgChartSeatEditRoleModel orgChartSeatEditRoleModel);
  Task<GraphQLResponse<CreateOrgChartPositionRoleOutputDTO>> CreateRoleChartSeat(OrgChartSeatCreateRoleModel orgChartSeatCreateRoleModel);
  Task<GraphQLResponseBase> CreateOrgChartSeat(OrgChartSeatCreateModel orgChartSeatCreateModel);
  Task<GraphQLResponseBase> DeleteRoleChartSeat(OrgChartSeatDeleteRoleModel orgChartSeatDeleteRoleModel);
  Task<GraphQLResponseBase> DeleteOrgChartSeat(OrgChartSeatDeleteModel orgChartSeatDeleteModel);
  #endregion
}

public partial class RadialReviewRepository
{
  public IQueryable<(long OrgChartId, OrgChartSeatQueryModel OrgChartSeat)> GetOrgChartSeatsForOrgCharts(IEnumerable<long> orgChartIds, CancellationToken cancellationToken)
  {
    // using var session = HibernateSession.GetCurrentSession();
    // using var tx = session.BeginTransaction();
    // dbContext.Database.SetDbConnection(session.Connection);

    var results =
        (
          from orgChart in dbContext.AccountabilityCharts
          from orgChartSeat in orgChart.Seats
          where orgChartIds.Contains(orgChart.Id)
          where orgChartSeat.DeleteTime == null
          where orgChart.RootId != orgChartSeat.Id
          select ValueTuple.Create(orgChart.Id, new OrgChartSeatQueryModel {
            Id = orgChartSeat.Id,
          })
        )
        ;

    // tx.Commit();
    return results;
  }

  public IQueryable<(long OrgChartSeatId, UserQueryModel User)> GetUsersForOrgChartSeats(IEnumerable<long> orgChartSeatIds, CancellationToken cancellationToken)
  {
    // using var session = HibernateSession.GetCurrentSession();
    // using var tx = session.BeginTransaction();
    // dbContext.Database.SetDbConnection(session.Connection);

    var results =
        (
          from orgChartSeat in dbContext.AccountabilityNodes /* aka OrgChartSeats */
          from seatuser in orgChartSeat.SeatUsers
          where orgChartSeatIds.Contains(orgChartSeat.Id)
          where seatuser.DeleteTime == null
          select ValueTuple.Create(orgChartSeat.Id, new UserQueryModel {
            Id = seatuser.UserOrganizationModel.ResponsibilityGroupModelId,
            FullName = seatuser.UserOrganizationModel.User.Fullname,
            FirstName = seatuser.UserOrganizationModel.User.FirstName,
            LastName = seatuser.UserOrganizationModel.User.LastName,
            Avatar = seatuser.UserOrganizationModel.User.ImageGuid // Assign ImageGuid in the avatar, then transform it into a valid URL in the AddUserAvatarInfo method.
          }.AddUserAvatarInfo())
        );

    // tx.Commit();
    return results;
  }

  public IQueryable<(long OrgChartSeatId, OrgChartPositionQueryModel Position)> GetPositionsForOrgChartSeats(IEnumerable<long> orgChartSeatIds, CancellationToken cancellationToken)
  {
    var results =
        (
          from orgChartSeat in dbContext.AccountabilityNodes /* aka OrgChartSeats */
          where orgChartSeatIds.Contains(orgChartSeat.Id)
          where orgChartSeat.Role.PositionName != null
          where orgChartSeat.DeleteTime == null
          select ValueTuple.Create(orgChartSeat.Id, new OrgChartPositionQueryModel {
            Id = orgChartSeat.Role.Id,
            Title = orgChartSeat.Role.PositionName
          })
        );

    return results;
  }

  public IQueryable<(long OrgChartSeatId, OrgChartSeatQueryModel OrgChartSeat)> GetDirectReportsForOrgChartSeats(IEnumerable<long> orgChartSeatIds, CancellationToken cancellationToken)
  {
    var results =
        (
          from orgChartSeat in dbContext.AccountabilityNodes /* aka OrgChartSeats */
          from directReport in orgChartSeat.DirectReports
          where orgChartSeatIds.Contains(orgChartSeat.Id)
          where directReport.DeleteTime == null
          select ValueTuple.Create(orgChartSeat.Id, new OrgChartSeatQueryModel {
            Id = directReport.Id,

          })
        );

    return results;
  }

  public async Task<GraphQLResponseBase> EditOrgChartSeat(OrgChartSeatEditModel orgChartSeatEditModel)
  {
    try
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        var accountabilityNode = s.Query<AccountabilityNode>().FirstOrDefault(a => a.Id == orgChartSeatEditModel.SeatId && a.DeleteTime == null);

        var orgChartAngular = orgChartSeatEditModel.ToAngularAccountabilityNode(accountabilityNode);

        await AccountabilityAccessor.Update(caller, orgChartAngular, null);

        // update parent
        if (orgChartSeatEditModel.SupervisorId != null)
        {
          await AccountabilityAccessor.SwapParents(caller, orgChartSeatEditModel.SeatId, orgChartSeatEditModel.SupervisorId.Value, null);
        }
      }

      return GraphQLResponseBase.Successfully();
    }
    catch
    {
      return GraphQLResponseBase.Error();
    }
  }

  public async Task<GraphQLResponseBase> EditRoleChartSeat(OrgChartSeatEditRoleModel orgChartSeatEditRoleModel)
  {
    try
    {
      var angularRole = orgChartSeatEditRoleModel.ToAngularRole();

      await AccountabilityAccessor.Update(caller, angularRole, null);

      return GraphQLResponseBase.Successfully();
    } catch (Exception ex)
    {
      return GraphQLResponseBase.Error();
    }
  }
  public async Task<GraphQLResponse<CreateOrgChartPositionRoleOutputDTO>> CreateRoleChartSeat(OrgChartSeatCreateRoleModel orgChartSeatCreateRoleModel)
  {
    try
    {
      var role = await AccountabilityAccessor.AddRole(caller, orgChartSeatCreateRoleModel.SeatId, orgChartSeatCreateRoleModel.Name);

      return GraphQLResponse<CreateOrgChartPositionRoleOutputDTO>.Successfully(new CreateOrgChartPositionRoleOutputDTO(role.Id, role.Name));
    }
    catch (Exception ex)
    {
      return GraphQLResponse<CreateOrgChartPositionRoleOutputDTO>.Error(ex);
    }

  }

  public async Task<GraphQLResponseBase> CreateOrgChartSeat(OrgChartSeatCreateModel orgChartSeatCreateModel)
  {
    try
    {
      long supervisorId;

      if (orgChartSeatCreateModel.SupervisorId == null)
      {
        var s = HibernateSession.GetCurrentSession();
        supervisorId = s.Query<AccountabilityChart>().FirstOrDefault(x => x.OrganizationId == caller.Organization.Id && x.DeleteTime == null).RootId;
      } else
      {
        supervisorId = orgChartSeatCreateModel.SupervisorId.Value;
      }

      await AccountabilityAccessor.AppendNode(
        caller,
        supervisorId,
        userIds: orgChartSeatCreateModel.userIds.ToList(),
        rolesToInclude: orgChartSeatCreateModel.roles.ToList(),
        positionName: orgChartSeatCreateModel.positionTitle,
        isRootNode: orgChartSeatCreateModel.SupervisorId == null
      );
      return GraphQLResponseBase.Successfully();
    } catch(Exception e)
    {
      return GraphQLResponseBase.Error();
    }
  }

  public async Task<List<OrgChartPositionRoleQueryModel>> GetPositionRoleForOrgChartSeat(long orgChartSeatId, CancellationToken cancellationToken)
  {
    var s = HibernateSession.GetCurrentSession();

    var nodeRoles = s.Query<AccountabilityNode>()
    .Where(node => node.AccountabilityRolesGroupId == orgChartSeatId && node.DeleteTime == null)
    .SelectMany(
      node => s.Query<SimpleRole>()
                         .Where(role => role.NodeId == node.Id && role.DeleteTime == null)
                         .Select(role => role.ToPositionRoleQueryModel())
    ).ToList();

    return nodeRoles;
  }

  public async Task<GraphQLResponseBase> DeleteRoleChartSeat(OrgChartSeatDeleteRoleModel orgChartSeatDeleteRoleModel)
  {
    try
    {
      await AccountabilityAccessor.RemoveRole(caller, orgChartSeatDeleteRoleModel.RoleId);
      return GraphQLResponseBase.Successfully();
    } catch(Exception ex)
    {
      return GraphQLResponseBase.Error(ex);
    }
  }

  public async Task<GraphQLResponseBase> DeleteOrgChartSeat(OrgChartSeatDeleteModel orgChartSeatDeleteModel)
  {
    try
    {
      await AccountabilityAccessor.RemoveNode(caller, orgChartSeatDeleteModel.SeatId);
      return GraphQLResponseBase.Successfully();
    } catch(Exception ex)
    {
      return GraphQLResponseBase.Error(ex);
    }
  }
}