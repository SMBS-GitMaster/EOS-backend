using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Models;
using RadialReview.Core.GraphQL.OrgChart.OrgChartSeat;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Askables;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GQL = RadialReview.GraphQL;
using RadialReview.Models;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_OrgChartSeat : IOrgChartSeatHook
  {
    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_OrgChartSeat(ITopicEventSender eventSender)
    {
      _eventSender = eventSender;
    }

    public bool AbsorbErrors()
    {
      return false;
    }

    public bool CanRunRemotely()
    {
      return false;
    }

    public HookPriority GetHookPriority()
    {
      return HookPriority.UI;
    }


    public async Task AttachOrgChartSeatUser(ISession s, UserOrganizationModel caller, AccountabilityNode node, long[] removedUser, long[] addUser)
    {
      var usersModel = UserAccessor.GetUsersByIdsUnsafe(addUser.Concat(removedUser).ToList(), s).ToDictionary(x => x.Id);
      var users = s.Query<UserOrganizationModel>().Where(x => x.Organization.Id == node.OrganizationId && x.DeleteTime == null).Select(x => x.Id).ToArray();

      foreach (var item in removedUser)
      {
        if (usersModel.TryGetValue(item, out var user))
        {
          var userQueryModel = user.TransformUser();

          await _eventSender.SendChangeAsync(ResourceNames.OrgChart(node.AccountabilityChartId), Change<IMeetingChange>.Removed(Change.Target(node.Id, OrgChartSeatQueryModel.Collections.User18.Users), item, userQueryModel)).ConfigureAwait(false);

          foreach (var u in users)
          {
            await _eventSender.SendChangeAsync(ResourceNames.User(u), Change<IMeetingChange>.Removed(Change.Target(node.Id, OrgChartSeatQueryModel.Collections.User18.Users), item, userQueryModel)).ConfigureAwait(false);
          }
        }
      }

      foreach (var item in addUser)
      {
        if (usersModel.TryGetValue(item, out var user))
        {
          var userQueryModel = user.TransformUser();
          await _eventSender.SendChangeAsync(ResourceNames.OrgChart(node.AccountabilityChartId), Change<IMeetingChange>.Inserted(Change.Target(node.Id, OrgChartSeatQueryModel.Collections.User18.Users), item, userQueryModel)).ConfigureAwait(false);

          foreach (var u in users)
          {
            await _eventSender.SendChangeAsync(ResourceNames.User(u), Change<IMeetingChange>.Inserted(Change.Target(node.Id, OrgChartSeatQueryModel.Collections.User18.Users), item, userQueryModel)).ConfigureAwait(false);
          }
        }
      }
    }

    public async Task AttachOrDetachOrgChartSeatSupervisor(ISession s, UserOrganizationModel caller, AccountabilityNode oldNode, AccountabilityNode newNode)
    {
      var users = s.Query<UserOrganizationModel>().Where(x => x.Organization.Id == newNode.OrganizationId && x.DeleteTime == null).Select(x => x.Id).ToArray();

      var oldUserIdsNode = oldNode.GetUsers(s).Select(x => x.Id).ToArray();
      var newUserIdsNode = newNode.GetUsers(s).Select(x => x.Id).ToArray();

      var oldNodeQueryModel =  oldNode.ToOrgChartSeatQueryModel();
      foreach (var userId in newUserIdsNode)
      {
        await _eventSender.SendChangeAsync(ResourceNames.OrgChart(newNode.AccountabilityChartId), Change<IMeetingChange>.Inserted(Change.Target(newNode.Id, OrgChartSeatQueryModel.Collections.OrgChartSeat.DirectReports), userId, oldNodeQueryModel)).ConfigureAwait(false);

        foreach (var u in users)
        {
          await _eventSender.SendChangeAsync(ResourceNames.User(userId), Change<IMeetingChange>.Inserted(Change.Target(newNode.Id, OrgChartSeatQueryModel.Collections.OrgChartSeat.DirectReports), userId, oldNodeQueryModel)).ConfigureAwait(false);
        }
      }

      var newNodeQueryModel = newNode.ToOrgChartSeatQueryModel();
      foreach (var userId in oldUserIdsNode)
      {
        await _eventSender.SendChangeAsync(ResourceNames.OrgChart(oldNode.AccountabilityChartId), Change<IMeetingChange>.Removed(Change.Target(oldNode.Id, OrgChartSeatQueryModel.Collections.OrgChartSeat.DirectReports), userId, newNodeQueryModel)).ConfigureAwait(false);

        foreach (var u in users)
        {
          await _eventSender.SendChangeAsync(ResourceNames.User(userId), Change<IMeetingChange>.Removed(Change.Target(oldNode.Id, OrgChartSeatQueryModel.Collections.OrgChartSeat.DirectReports), userId, newNodeQueryModel)).ConfigureAwait(false);
        }
      }
    }

    public async Task CreateOrgChartSeat(ISession s, UserOrganizationModel caller, AccountabilityNode node, bool rootNode = false)
    {
      var users = s.Query<UserOrganizationModel>().Where(x => x.Organization.Id == node.OrganizationId && x.DeleteTime == null).Select(x => x.Id).ToArray();
      var orgChartSeat = node.ToOrgChartSeatQueryModel();
      var accChartId = node.AccountabilityChartId;
      var parentNodeId = node.ParentNode.Id;

      foreach (var userId in users)
      {
        await _eventSender.SendChangeAsync(ResourceNames.User(userId), Change<IMeetingChange>.Inserted(Change.Target(accChartId, GQL.Models.OrgChartQueryModel.Collections.OrgChartSeat2.Seats), accChartId, orgChartSeat)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.OrgChart(accChartId), Change<IMeetingChange>.Inserted(Change.Target(accChartId, GQL.Models.OrgChartQueryModel.Collections.OrgChartSeat2.Seats), accChartId, orgChartSeat)).ConfigureAwait(false);

        if(!rootNode)
        {
          await _eventSender.SendChangeAsync(ResourceNames.User(userId), Change<IMeetingChange>.Inserted(Change.Target(parentNodeId, GQL.Models.OrgChartSeatQueryModel.Collections.OrgChartSeat.DirectReports), parentNodeId, node.ToOrgChartSeatQueryModel())).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.OrgChart(accChartId), Change<IMeetingChange>.Inserted(Change.Target(parentNodeId, GQL.Models.OrgChartSeatQueryModel.Collections.OrgChartSeat.DirectReports), parentNodeId, node.ToOrgChartSeatQueryModel())).ConfigureAwait(false);
        }
      }
    }

    public async Task UpdateOrgChartSeat(ISession s, UserOrganizationModel caller, AccountabilityNode node, IOrgChartSeatHookUpdates updates)
    {
      if (updates.PositionTitle)
      {
        var users = s.Query<UserOrganizationModel>().Where(x => x.Organization.Id == node.OrganizationId && x.DeleteTime == null).Select(x => x.Id).ToArray();
        var nodeRole = s.Query<SimpleRole>().Where(x => x.NodeId == node.Id && x.DeleteTime == null).ToList();

        node.AccountabilityRolesGroup._Roles = new List<RoleGroup>()
        {
          new()
          {
            Roles = nodeRole
          }
        };

        var source = node.ToPositionQueryModel();

        foreach (var userId in users)
        {
          await _eventSender.SendChangeAsync(ResourceNames.User(userId), Change<IMeetingChange>.Updated(source.Id, source, null)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.OrgChart(node.AccountabilityChartId), Change<IMeetingChange>.Updated(source.Id, source, null)).ConfigureAwait(false);
        }
      }
    }

    public async Task CreateOrgChartSeatRole(ISession s, UserOrganizationModel caller, SimpleRole role)
    {
      var r = s.Query<SimpleRole>().FirstOrDefault(x => x.Id == role.Id && x.DeleteTime == null);
      var node = s.Query<AccountabilityNode>().FirstOrDefault(x => x.Id == r.NodeId && x.DeleteTime == null);
      var positionId = node.AccountabilityRolesGroupId;
      var users = s.Query<UserOrganizationModel>().Where(x => x.Organization.Id == r.OrgId && x.DeleteTime == null).Select(x => x.Id).ToArray();
      var positionRolQueryModel = r.ToOrgChartSeatRoleQueryModel();

      foreach (var userId in users)
      {
        await _eventSender.SendChangeAsync(ResourceNames.User(userId), Change<IMeetingChange>.Inserted(Change.Target(positionId, GQL.Models.OrgChartPositionQueryModel.Collections.OrgChartPositionRole.Roles), positionId, positionRolQueryModel)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.OrgChart(node.AccountabilityChartId), Change<IMeetingChange>.Inserted(Change.Target(positionId, GQL.Models.OrgChartPositionQueryModel.Collections.OrgChartPositionRole.Roles), positionId, positionRolQueryModel)).ConfigureAwait(false);
      }
    }

    public async Task UpdateOrgChartSeatRole(ISession s, UserOrganizationModel caller, long roleId)
    {
      var r = s.Query<SimpleRole>().FirstOrDefault(x => x.Id == roleId && x.DeleteTime == null);
      var users = s.Query<UserOrganizationModel>().Where(x => x.Organization.Id == r.OrgId && x.DeleteTime == null).Select(x => x.Id).ToArray();

      foreach (var userId in users)
      {
        await _eventSender.SendChangeAsync(ResourceNames.User(userId), Change<IMeetingChange>.Updated(r.Id, r.ToOrgChartSeatRoleQueryModel(), null)).ConfigureAwait(false);
        // await _eventSender.SendChangeAsync(ResourceNames.OrgChart(orgChartId), Change<IMeetingChange>.Updated(r.Id, r.ToOrgChartSeatRoleQueryModel(), null)).ConfigureAwait(false);
      }
    }

    public async Task DeleteOrgChartSeatRole(ISession s, UserOrganizationModel caller, long roleId)
    {
      var r = s.Query<SimpleRole>().FirstOrDefault(x => x.Id == roleId);
      var users = s.Query<UserOrganizationModel>().Where(x => x.Organization.Id == r.OrgId && x.DeleteTime == null).Select(x => x.Id).ToArray();
      var node = s.Query<AccountabilityNode>().FirstOrDefault(x => x.Id == r.NodeId && x.DeleteTime == null);
      var positionId = node.AccountabilityRolesGroupId;

      foreach (var userId in users)
      {
        await _eventSender.SendChangeAsync(ResourceNames.User(userId), Change<IMeetingChange>.Removed(Change.Target(positionId, GQL.Models.OrgChartPositionQueryModel.Collections.OrgChartPositionRole.Roles), r.Id, r.ToOrgChartSeatRoleQueryModel())).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.OrgChart(node.AccountabilityChartId), Change<IMeetingChange>.Removed(Change.Target(positionId, GQL.Models.OrgChartPositionQueryModel.Collections.OrgChartPositionRole.Roles), r.Id, r.ToOrgChartSeatRoleQueryModel())).ConfigureAwait(false);
      }
    }

    public async Task DeleteOrgChartSeat(ISession s, AccountabilityNode node)
    {
      var users = s.Query<UserOrganizationModel>().Where(x => x.Organization.Id == node.OrganizationId && x.DeleteTime == null).Select(x => x.Id).ToArray();
      var orgChartSeat = node.ToOrgChartSeatQueryModel();
      var accChartId = node.AccountabilityChartId;
      var parentNodeId = node.ParentNode.Id;
      var orgChartRootNode = s.Query<AccountabilityChart>().FirstOrDefault(x => x.Id == accChartId && x.DeleteTime == null).RootId;
      var isRootNode = parentNodeId == orgChartRootNode;

      foreach (var userId in users)
      {
        await _eventSender.SendChangeAsync(ResourceNames.User(userId), Change<IMeetingChange>
          .Removed(Change.Target(accChartId, GQL.Models.OrgChartQueryModel.Collections.OrgChartSeat2.Seats), node.Id, orgChartSeat))
          .ConfigureAwait(false);

        await _eventSender.SendChangeAsync(ResourceNames.OrgChart(accChartId), Change<IMeetingChange>
          .Removed(Change.Target(accChartId, GQL.Models.OrgChartQueryModel.Collections.OrgChartSeat2.Seats), node.Id, orgChartSeat))
          .ConfigureAwait(false);

        if (!isRootNode)
        {
          await _eventSender.SendChangeAsync(ResourceNames.User(userId), Change<IMeetingChange>.Removed(Change.Target(parentNodeId, GQL.Models.OrgChartSeatQueryModel.Collections.OrgChartSeat.DirectReports), node.Id, orgChartSeat)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.OrgChart(accChartId), Change<IMeetingChange>.Removed(Change.Target(parentNodeId, GQL.Models.OrgChartSeatQueryModel.Collections.OrgChartSeat.DirectReports), node.Id, orgChartSeat)).ConfigureAwait(false);

        }
      }
    }
  }
}
