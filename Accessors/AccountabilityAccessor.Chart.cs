using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static RadialReview.Models.PermItem;

namespace RadialReview.Accessors {
  public partial class AccountabilityAccessor : BaseAccessor {
    public const string CREATE_TEXT = " (Create)";

    public static long GetOrganizationChartId(UserOrganizationModel caller, long orgId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var org = s.Get<OrganizationModel>(orgId);
          perms.CanView(ResourceType.AccountabilityHierarchy, org.AccountabilityChartId);

          return org.AccountabilityChartId;
        }
      }
    }
    public static AccountabilityNode GetRoot(ISession s, PermissionsUtility perms, long chartId) {
      var c = s.Get<AccountabilityChart>(chartId);
      perms.ViewOrganization(c.OrganizationId);
      return s.Get<AccountabilityNode>(c.RootId);
    }
    public static AccountabilityNode GetRoot(UserOrganizationModel caller, long chartId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetRoot(s, perms, chartId);

        }
      }
    }
    public static AccountabilityChart CreateChart(UserOrganizationModel caller, long organizationId, bool creatorCanAdmin = true) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);

          var chart = CreateChart(s, perms, organizationId, creatorCanAdmin);

          tx.Commit();
          s.Flush();

          return chart;

        }
      }
    }
    public static AccountabilityChart CreateChart(ISession s, PermissionsUtility perms, long organizationId, bool creatorCanAdmin = true) {
      perms.ViewOrganization(organizationId);
      var now = DateTime.UtcNow;

      var chart = new AccountabilityChart() {
        OrganizationId = organizationId,
        Name = s.Get<OrganizationModel>(organizationId).GetName(),
        CreateTime = now,
      };
      s.Save(chart);

      var group = new AccountabilityRolesGroup() {
        OrganizationId = organizationId,
        AccountabilityChartId = chart.Id,
        CreateTime = now,
      };
      s.Save(group);

      var root = new AccountabilityNode() {
        OrganizationId = organizationId,
        ParentNodeId = null,
        ParentNode = null,
        AccountabilityRolesGroupId = group.Id,
        AccountabilityRolesGroup = group,
        AccountabilityChartId = chart.Id,
        CreateTime = now,
      };
      s.Save(root);

      chart.RootId = root.Id;
      s.Update(chart);

      PermissionsAccessor.InitializePermItems_Unsafe(s, perms.GetCaller(), PermItem.ResourceType.AccountabilityHierarchy, chart.Id,
        PermTiny.Admins(),
        PermTiny.Creator(view: creatorCanAdmin, edit: creatorCanAdmin, admin: creatorCanAdmin),
        PermTiny.Members(edit: false, admin: false)
      );

      return chart;
    }

    public static AngularAccountabilityChart GetTree(UserOrganizationModel caller, long chartId, DateRange range = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetTree(s, perms, chartId, range);
        }
      }
    }

    public static AngularAccountabilityChart GetTree(ISession s, PermissionsUtility perms, long chartId, DateRange range = null) {

      //Ensure Permitted
      perms.ViewHierarchy(chartId);
      var chart = s.Get<AccountabilityChart>(chartId);

      //What's allowed?
      var editAC = perms.IsPermitted(x => x.EditHierarchy(chart.Id));
      var editAll = perms.IsPermitted(x => x.Or(y => y.ManagingOrganization(chart.OrganizationId), y => y.EditHierarchy(chart.Id)));
      //Seats can contain multiple users now.
      var editSelf = perms.GetCaller().Organization.Settings.EmployeesCanEditSelf;
      if (perms.GetCaller().IsManager()) {
        editSelf = editSelf || perms.GetCaller().Organization.Settings.ManagersCanEditSelf;
      }

      //Future Queries
      var nodesQ = s.QueryOver<AccountabilityNode>().Where(x => x.AccountabilityChartId == chartId).Where(range.Filter<AccountabilityNode>()).Future();
      var groupsQ = s.QueryOver<AccountabilityRolesGroup>().Where(x => x.AccountabilityChartId == chartId).Where(range.Filter<AccountabilityRolesGroup>()).Future();
      var rolesQ = s.QueryOver<SimpleRole>().Where(x => x.OrgId == chart.OrganizationId).Where(range.Filter<SimpleRole>()).Future();
      var usersF = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == chart.OrganizationId && !x.IsClient).Where(range.Filter<UserOrganizationModel>()).List().ToList();
      var userLookupsQ = s.QueryOver<UserLookup>().Where(x => x.OrganizationId == chart.OrganizationId).Where(range.Filter<UserLookup>()).Future();
      AccountabilityNode nodeAlias = null;
      var accNodeUsersQ = s.QueryOver<AccountabilityNodeUserMap>().JoinAlias(x => x.AccountabilityNode, () => nodeAlias).Where(x => x.ChartId == chartId).Where(range.Filter<AccountabilityNodeUserMap>());
      if (range == null) {
        accNodeUsersQ = accNodeUsersQ.Where(_ => nodeAlias.DeleteTime == null);
      } else {
        accNodeUsersQ = accNodeUsersQ.Where(_ => nodeAlias.CreateTime <= range.EndTime && (nodeAlias.DeleteTime == null || nodeAlias.DeleteTime >= range.StartTime));
      }
      var accNodeUsersF = accNodeUsersQ.Fetch(x => x.User).Eager.Future();


      //Resolve
      var nodes = nodesQ.ToList();
      var groups = groupsQ.ToList();
      var roles = rolesQ.ToList();
      var userLookups = userLookupsQ.ToList();
      var accNodeUsers = accNodeUsersF.ToList();

      foreach (var node in nodes) {
        node.SetUsers(accNodeUsers);
      }

      var nameLookup = new Dictionary<long, string>();
      foreach (var anu in accNodeUsers.Select(x => x.User)) {
        nameLookup[anu.Id] = anu.GetName();
      }

      var allManaging = new HashSet<long>();

      var root = Dive(perms.GetCaller(), chart.RootId, nodes, groups, roles, allManaging, editAll, editSelf);

      var allUsers = usersF.ToList().Select(x =>
        AngularUser.CreateUser(x, managing: editAll || allManaging.Contains(x.Id) || (perms.GetCaller().IsManager() && perms.GetCaller().Id == x.Id))
      ).ToList();

      var c = new AngularAccountabilityChart(chartId) {
        Root = root,
        AllUsers = allUsers,
        CanReorganize = editAC,
      };
      c.Root.Name = chart.Name;
      return c;
    }

    protected static AngularAccountabilityNode Dive(UserOrganizationModel caller, long nodeId, List<AccountabilityNode> nodes,
        List<AccountabilityRolesGroup> groups, List<SimpleRole> allRoles, HashSet<long> allManagingUserIds,
         bool? editableBelow, bool editSelf) {
      var me = nodes.FirstOrDefault(x => x.Id == nodeId);
      var children = nodes.Where(x => x.ParentNodeId == nodeId).ToList();
      //Calculate Permissions
      var isEditable = false;
      if (me.ContainsOnlyUser(caller.Id) && !children.Any() && !caller.ManagingOrganization) {
        isEditable = caller.Organization.Settings.EmployeesCanEditSelf;
      } else if (editableBelow == true) {
        isEditable = true;
      }

      var isMe = false;
      var isMeExclusive = false;

      if (editableBelow != null && me.ContainsUser(caller.Id)) {
        editableBelow = true;
        isMe = true;
        if (me.ContainsOnlyUser(caller.Id)) {
          isMeExclusive = true;
        }
      }

      if (isMeExclusive && editSelf) {
        isEditable = true;
      }

      var group = groups.First(x => x.Id == me.GetAccountabilityRolesGroupId());

      List<AngularRoleGroup> roleGroups = allRoles.Where(x => x.NodeId == nodeId)
                  .GroupBy(x => ("" + AttachType.Node))
                   .Select(roles => {
                     var rr = roles.Select(role => new AngularRole(role)).ToList();
                     return AngularRoleGroup.CreateForNode(nodeId, rr, isEditable);
                   }).ToList();

      if (!roleGroups.Any(x => x.AttachType == AttachType.Node)) {
        roleGroups.Add(AngularRoleGroup.CreateForNode(nodeId, new List<AngularRole>(), isEditable));
      }

      var aaGroup = new AngularAccountabilityGroup(group.Id, group.PositionName, roleGroups, editable: isEditable);
      var aan = new AngularAccountabilityNode() {
        Id = nodeId,
        Users = me.GetUsers(null).Select(x => AngularUser.CreateUser(x)),
        Group = aaGroup,
        collapsed = true,
        Editable = isEditable,
        Me = isMe,
        order = me.Ordering,
      };

      if (isEditable) {
        foreach (var u in me.GetUsers(null)) {
          allManagingUserIds.Add(u.Id);
        }
      }


      aan.SetChildren(children.Select(x => Dive(caller, x.Id, nodes, groups, allRoles, allManagingUserIds, editableBelow, editSelf)).ToList());
      return aan;
    }

    public static async Task _FinishUploadAccountabilityChart(UserOrganizationModel caller, List<UserOrganizationModel> addedUsers, List<UserOrganizationModel> existingUsers, Dictionary<long, string[]> managerLookup, CounterSet<string> errors) {
      var nodeLookup = new Dictionary<long, AccountabilityNode>();
      var allSet = new HashSet<long>(existingUsers.Select(x => x.Id));
      var addedSet = new HashSet<long>(addedUsers.Select(x => x.Id));

      //[Manager_UserOrgId,DirectReport_UserId]
      var managerToDirectReportLinks = new HashSet<Tuple<long, long>>(managerLookup.Select(m => {
        var manager = existingUsers.FirstOrDefault(x => x.GetFirstName() == m.Value[0] && x.GetLastName() == m.Value[1]);
        if (manager == null)
          return null;
        return Tuple.Create(manager.Id, m.Key);
      }).Where(x => x != null));

      var sorted_uids = GraphUtility.TopologicalSort(allSet, managerToDirectReportLinks);

      if (sorted_uids == null) {
        throw new PermissionsException("Circular reference detected! ");
      }

      var toAddNodes_uids = addedSet.ToList();

      var root = AccountabilityAccessor.GetRoot(caller, caller.Organization.AccountabilityChartId);

      //Add them to the top row of AC if there isnt a link
      foreach (var uid in addedSet.Where(x => !managerToDirectReportLinks.Any(y => y.Item2 == x))) {
        toAddNodes_uids.Remove(uid);
        nodeLookup[uid] = await AccountabilityAccessor.AppendNode(caller, root.Id, userIds: new List<long> { uid });
      }


      foreach (var managerId in sorted_uids) {
        foreach (var link in managerToDirectReportLinks.Where(x => x.Item1 == managerId)) {
          var subId = link.Item2;
          //Should we add this user?
          if (toAddNodes_uids.Any(x => x == subId)) {
            toAddNodes_uids.Remove(subId);
            //Find the manager
            var foundManager = existingUsers.FirstOrDefault(x => x.Id == managerId);
            if (!foundManager.ManagerAtOrganization) {
              await UserAccessor.EditUser(caller, foundManager.Id, true);
              foundManager.ManagerAtOrganization = true;
            }
            //pick a manager node to add the user to...
            long? managerNodeId = null;
            if (nodeLookup.ContainsKey(managerId)) {
              managerNodeId = nodeLookup[managerId].Id;
            } else {
              var mNode = DeepAccessor.Users.GetNodesForUser(caller, managerId).FirstOrDefault();
              if (mNode != null)
                managerNodeId = mNode.Id;
            }
            //Add the user to the manager node.
            if (managerNodeId != null) {
              nodeLookup[subId] = await AccountabilityAccessor.AppendNode(caller, managerNodeId.Value, userIds: new List<long> { subId });
            } else {
              errors.Add("Could not create accountability node.");
            }
          }
        }
      }
    }

    #region Single call
    [Untested("StrictlyAfter", "Both angularTypes")]
    public static async Task Update(UserOrganizationModel caller, IAngularId model, string connectionId) {


      if (model.GetAngularType() == typeof(AngularAccountabilityNode).Name) {
        using (var usersToUpdate = new UserCacheUpdater()) {
          using (var s = HibernateSession.GetCurrentSession()) {
            using (var tx = s.BeginTransaction()) {
              await using (var rt = RealTimeUtility.Create(connectionId)) {
                var perms = PermissionsUtility.Create(s, caller);
                var m = (AngularAccountabilityNode)model;
                await UpdateAccountabilityNode(s, rt, perms, m.Id, m.Group.NotNull(x => x.Position.Name), m.Users.NotNull(x => x.SelectId().ToList()), usersToUpdate);
                tx.Commit();
                s.Flush();
              }
            }
          }
        }
      } else if (model.GetAngularType() == typeof(AngularRole).Name) {
        var m = (AngularRole)model;
        await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateRole(m.Id), async (s,perms) => {
          perms.EditSimpleRole(m.Id);
        }, async s => {
          await UpdateRole_Unsafe(s, caller, m.Id, m.Name);
        }, null);
      } else {
        throw new PermissionsException("Unhandled type: " + model.GetAngularType());
      }

    }

    #endregion


  }
}
