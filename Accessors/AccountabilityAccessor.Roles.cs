using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate.Criterion;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;

namespace RadialReview.Accessors {
  public partial class AccountabilityAccessor : BaseAccessor {

    public static List<SimpleRole> GetRolesForUser(UserOrganizationModel caller, long userId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetRolesForUser(s, perms, userId);
        }
      }
    }

    public static List<SimpleRole> GetRolesForUser(ISession s, PermissionsUtility perms, long userId) {
      perms.ViewUserOrganization(userId, false);
      var nodesForUser = DeepAccessor.Unsafe.Criterions.SelectNodeIdsGivenUser_Unsafe(userId);
      return s.QueryOver<SimpleRole>()
        .Where(x => x.DeleteTime == null)
        .WithSubquery.WhereProperty(x => x.NodeId).In(nodesForUser)
        .List().ToList();
    }

    public static SimpleRole GetRole(UserOrganizationModel caller, long roleId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        var perms = PermissionsUtility.Create(s, caller);
        perms.ViewSimpleRole(roleId);
        var roles = s.Get<SimpleRole>(roleId);
        if (roles.DeleteTime != null) {
          throw new PermissionsException("Cannot view role.");
        }
        return roles;
      }
    }
    public static async Task UpdateRole(UserOrganizationModel caller, long roleId, string name = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.EditSimpleRole(roleId);
          await UpdateRole_Unsafe(OrderedSession.Indifferent(s), perms.GetCaller(), roleId, name);
          tx.Commit();
          s.Flush();
        }
      }

    }



    public static async Task UpdateRole_Unsafe(IOrderedSession s, UserOrganizationModel caller, long roleId, string name = null) {

      var role = s.Get<SimpleRole>(roleId);
      var updates = new ISimpleRoleHookUpdates();
      var anyChange = false;

      name = name?.Replace("&amp;", "&");
      if (name != null && name != role.Name) {
        role.Name = name;
        s.Update(role);
        updates.NameChanged = true;
        anyChange = true;
      }

      if (anyChange) {
        await HooksRegistry.Each<ISimpleRoleHook>((ses, x) => x.UpdateRole(ses, roleId, updates));
        await HooksRegistry.Each<IOrgChartSeatHook>((ses, x) => x.UpdateOrgChartSeatRole(ses, caller, role.Id));
      }
    }

    [Todo]
    public static List<AngularRole> GetRolesForSeat(UserOrganizationModel caller, long nodeId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewAccountabilityNode(nodeId);

          var roles = s.QueryOver<SimpleRole>()
            .Where(x => x.DeleteTime == null && x.NodeId == nodeId)
            .List()
            .ToList();
          return roles.Select(x => new AngularRole(x)).ToList();
        }
      }
    }
    public static async Task<List<AngularRole>> GetRolesForSeat(ISession s, PermissionsUtility perms, long nodeId) {

      perms.ViewAccountabilityNode(nodeId);

      var roles = s.QueryOver<SimpleRole>()
        .Where(x => x.DeleteTime == null && x.NodeId == nodeId)
        .List()
        .ToList();
      return roles.Select(x => new AngularRole(x)).ToList();


    }
    public static async Task<SimpleRole> AddRole(UserOrganizationModel caller, long nodeId, string name = null, int? insert = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await using (var rt = RealTimeUtility.Create()) {

            var perms = PermissionsUtility.Create(s, caller);
            var res = await AddRole(s, perms, rt, nodeId, name, insert);
            tx.Commit();
            s.Flush();

            await HooksRegistry.Each<IOrgChartSeatHook>((ses, x) => x.CreateOrgChartSeatRole(ses, perms.GetCaller(), res));
            return res;
          }
        }
      }
    }
    [Todo]
    public static async Task<SimpleRole> AddRole(ISession s, PermissionsUtility perms, RealTimeUtility rt, long nodeId, string name, int? insert) {

      perms.ManagesAccountabilityNodeOrSelf(nodeId);
      var node = s.Get<AccountabilityNode>(nodeId);
      var maxOrderO = s.CreateCriteria<SimpleRole>()
        .Add(Expression.Eq(Projections.Property<SimpleRole>(x => x.DeleteTime), null))
        .Add(Expression.Eq(Projections.Property<SimpleRole>(x => x.NodeId), nodeId))
        .SetProjection(Projections.Max(Projections.Property<SimpleRole>(x => x.Ordering)), Projections.Count(Projections.Property<SimpleRole>(x => x.Ordering)))
        .UniqueResult<object[]>();

      List<SimpleRole> allNodeRoles = null;
      if (insert != null) {
        allNodeRoles = s.QueryOver<SimpleRole>().Where(x => x.DeleteTime == null && x.NodeId == nodeId).List().ToList();
      }

      var maxOrder = 0;
      var count = (int)maxOrderO[1];
      if (count != 0) {
        maxOrder = (int)maxOrderO[0] + 1;
      }

      var r = new SimpleRole() {
        CreateTime = DateTime.UtcNow,
        Name = name,
        NodeId = nodeId,
        OrgId = node.OrganizationId,
        AttachType_Deprecated = "Node",
        Ordering = maxOrder,
      };
      s.Save(r);
      await HooksRegistry.Each<ISimpleRoleHook>((ses, x) => x.CreateRole(ses, r.Id));

      if (insert != null && insert != count) {
        ApplyInsert(s, allNodeRoles, r, insert.Value, x => x.Ordering, async meta => {
          await HooksRegistry.Each<ISimpleRoleHook>((ses, x) => x.UpdateRole(ses, meta.Id, new ISimpleRoleHookUpdates() { OrderingChanged = true }));
        });
      }

      var orgUpdater = rt.UpdateOrganization(node.OrganizationId);
      var updatedRoles = AngularList.CreateFrom(AngularListType.Add, new AngularRole(r));
      orgUpdater.Update(new AngularRoleGroup(new Attach(AttachType.Node, nodeId), updatedRoles), insert: 0);

      return r;
    }


    [Obsolete("todo")]
    public static async Task RemoveRole(UserOrganizationModel caller, long roleId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          await RemoveRole(s, perms, roleId);
          tx.Commit();
          s.Flush();

        }
      }
    }

    [Obsolete("todo")]
    public static async Task RemoveRole(ISession s, PermissionsUtility perms, long roleId) {
      perms.EditSimpleRole(roleId);
      var role = s.Get<SimpleRole>(roleId);
      if (role.DeleteTime != null)
        throw new PermissionsException("Role already deleted");
      role.DeleteTime = DateTime.UtcNow;
      s.Update(role);
      await HooksRegistry.Each<ISimpleRoleHook>((ses, x) => x.UpdateRole(ses, roleId, new ISimpleRoleHookUpdates() {
        DeleteTimeChanged = true,
      }));

      await HooksRegistry.Each<IOrgChartSeatHook>((ses, x) => x.DeleteOrgChartSeatRole(ses, perms.GetCaller(), role.Id));
    }
    public static async Task RestoreRole(UserOrganizationModel caller, long simpleRoleId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          //Shouldnt this be equivalent to UnremoveRole??
          PermissionsUtility.Create(s, caller).EditSimpleRole(simpleRoleId);
          var r = s.Get<SimpleRole>(simpleRoleId);
          if (r.DeleteTime == null) {
            throw new PermissionsException("Role already exists");
          }
          r.DeleteTime = null;
          s.Update(r);

          tx.Commit();
          s.Flush();
          await HooksRegistry.Each<ISimpleRoleHook>((ses, x) => x.UpdateRole(ses, simpleRoleId, new ISimpleRoleHookUpdates() {
            DeleteTimeChanged = true,
          }));
        }
      }
    }

    public partial class Unsafe {
      public static RoleLinksQuery GetRolesQueryForOrganization_Unsafe(ISession s, long orgId) {

        var roleQ = s.QueryOver<SimpleRole>().Where(x => x.DeleteTime == null && x.OrgId == orgId).Future();
        var nodeUserMapQ = s.QueryOver<AccountabilityNodeUserMap>().Where(x => x.DeleteTime == null && x.OrgId == orgId).Future();
        return new RoleLinksQuery(roleQ, nodeUserMapQ);
      }

      public static int CountRoles_Unsafe(ISession s, long userId) {
        var nodesForUser = DeepAccessor.Unsafe.Criterions.SelectNodeIdsGivenUser_Unsafe(userId);
        return s.QueryOver<SimpleRole>()
          .Where(x => x.DeleteTime == null)
          .WithSubquery.WhereProperty(x => x.NodeId).In(nodesForUser)
          .RowCount();
      }
    }
  }

  public class RoleLinksQuery {
    protected IEnumerable<SimpleRole> Roles { get; set; }
    protected IEnumerable<AccountabilityNodeUserMap> UserMaps { get; set; }

    public RoleLinksQuery(IEnumerable<SimpleRole> roles, IEnumerable<AccountabilityNodeUserMap> userMaps) {
      Roles = roles;
      UserMaps = userMaps;
    }

    public IEnumerable<SimpleRole> GetRoleDetailsForUser(long forUserId) {
      // This one probably needs to be changed for multi-user per node
      var nodeIdsForUser = UserMaps.Where(x => x.UserId == forUserId).Select(x => x.AccountabilityNodeId).Distinct().ToList();
      return Roles.Where(x => nodeIdsForUser.Contains(x.NodeId)).ToList();
    }

    public IEnumerable<SimpleRole> GetRoleDetailsForNode(AccountabilityNode node) {
      //Probably doesn't workg anymore now that there are multiple users per node	??
      // This one probably needs to be changed for multi-user per node
      return Roles.Where(x => node.Id == x.NodeId).ToList();

    }
  }
}
