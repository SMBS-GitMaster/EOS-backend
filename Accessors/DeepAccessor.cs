using NHibernate;
using NHibernate.Criterion;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Models.Accountability;
using FluentNHibernate.Mapping;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.UserModels;
using NHibernate.SqlCommand;
using NHibernate.Engine;
using NHibernate.Type;
using RadialReview.Core.Utilities;

namespace RadialReview.Accessors {
  public class DeepAccessor : BaseAccessor {
    #region Helpers
    public class Dive {
      public static List<AccountabilityNode> GetSubordinates(ISession s, AccountabilityNode manager, int levels = int.MaxValue) {
        var alreadyHit = new List<long>();
        return Children(s, manager, new List<String> { "" + manager.Id }, levels, 0, alreadyHit);
      }
      public static List<AccountabilityNode> GetSuperiors(ISession s, AccountabilityNode manager, int levels = int.MaxValue) {
        var alreadyHit = new List<long>();
        return Parents(s, manager, new List<String> { "" + manager.Id }, levels, 0, alreadyHit);
      }

      private static List<AccountabilityNode> Children(ISession s, AccountabilityNode parent, List<String> parents, int levels, int level, List<long> alreadyHit) {
        var children = new List<AccountabilityNode>();
        if (levels <= 0)
          return children;
        levels = levels - 1;

        parent = s.Get<AccountabilityNode>(parent.Id);


        children = s.QueryOver<AccountabilityNode>()
          .Where(x => x.DeleteTime == null && x.ParentNodeId == parent.Id)
          .List().ToList();


        if (!children.Any())
          return children;
        var iter = children.ToList();
        foreach (var c in iter) {
          if (!alreadyHit.Contains(c.Id)) {
            alreadyHit.Add(c.Id);
            children.Add(c);
            var copy = parents.Select(x => x).ToList();
            copy.Add("" + c.Id);
            children.AddRange(Children(s, c, copy, levels, level + 1, alreadyHit));
          }
        }
        return children;
      }

      private static List<AccountabilityNode> Parents(ISession s, AccountabilityNode child, List<String> children, int levels, int level, List<long> alreadyHit) {
        var parents = new List<AccountabilityNode>();
        if (levels <= 0)
          return parents;
        levels = levels - 1;
        child = s.Get<AccountabilityNode>(child.Id);

        if (child.ParentNode != null)
          parents.Add(child.ParentNode);



        if (parents.Count == 0)
          return parents;
        var c = child.ParentNode;
        if (!alreadyHit.Contains(c.Id)) {
          alreadyHit.Add(c.Id);
          parents.Add(c);
          var copy = children.Select(x => x).ToList();
          copy.Add("" + c.Id);
          parents.AddRange(Parents(s, c, copy, levels, level + 1, alreadyHit));
        }
        return parents;
      }
    }

    public class Tiny {

      private static Func<object[], TinyUser> Unpackage = new Func<object[], TinyUser>(x => {
        var fname = (string)x[0];
        var lname = (string)x[1];
        var email = (string)x[5];
        var uoId = (long)x[2];
        if (fname == null && lname == null) {
          fname = (string)x[3];
          lname = (string)x[4];
          email = (string)x[6];
        }
        return new TinyUser() {
          FirstName = fname,
          LastName = lname,
          Email = email,
          UserOrgId = uoId
        };
      });

      public static List<TinyUser> GetSubordinatesAndSelf(UserOrganizationModel caller, long userId) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var ids = Users.GetSubordinatesAndSelf(s, caller, userId).ToArray();

            TempUserModel tempUserAlias = null;
            UserOrganizationModel userOrgAlias = null;
            UserModel userAlias = null;

            return s.QueryOver<UserOrganizationModel>(() => userOrgAlias)
              .Left.JoinAlias(x => x.User, () => userAlias)
              .Left.JoinAlias(x => x.TempUser, () => tempUserAlias)
              .Where(x => x.DeleteTime == null)
              .WhereRestrictionOn(x => x.Id).IsIn(ids)
              .Select(x => userAlias.FirstName, x => userAlias.LastName, x => x.Id, x => tempUserAlias.FirstName, x => tempUserAlias.LastName, x => userAlias.UserName, x => tempUserAlias.Email)
              .List<object[]>()
              .Select(Unpackage)
              .ToList();

          }
        }
      }

    }


    public class Users {
      public class DeleteRecord {
        public virtual long Id { get; set; }
        public virtual DateTime Time { get; set; }
        public virtual long UserId { get; set; }
        public virtual long NodeId { get; set; }
        public virtual DateTime? DeleteTime { get; set; }

        public class Map : ClassMap<DeleteRecord> {
          public Map() {
            Id(x => x.Id);
            Map(x => x.Time);
            Map(x => x.UserId);
            Map(x => x.NodeId);
            Map(x => x.DeleteTime);
          }
        }
      }

      [Todo]
      public static void DeleteAll_Unsafe(ISession s, UserOrganizationModel forUser, DateTime? deleteTime = null) {

        var maps = s.QueryOver<AccountabilityNodeUserMap>()
                .Where(x => x.UserId == forUser.Id && x.DeleteTime == null)
                .List().ToList();


        deleteTime = deleteTime ?? DateTime.UtcNow;

        foreach (var a in maps) {
          a.DeleteTime = deleteTime;
          s.Update(a);

        }
      }
      [Todo]
      public static bool UndeleteAll_Unsafe(ISession s, UserOrganizationModel forUser, DateTime deleteTime, ref List<String> messages) {
        if (messages == null)
          messages = new List<String>();


        var maps = s.QueryOver<AccountabilityNodeUserMap>()
                .Where(x => x.UserId == forUser.Id && x.DeleteTime == deleteTime)
                .List().ToList();

        var count = 0;
        var success = 0;
        var allSuccess = true;
        foreach (var a in maps) {
          //If has children and has user, error.

          var node = a.AccountabilityNode;
          AccountabilityNode nodeAlias = null;
          var hasChildrenF = s.QueryOver<AccountabilityNode>().Where(x => x.DeleteTime == null && x.ParentNodeId == node.Id).Select(x => x.Id).Take(1).Future<long>();
          var hasUserF = s.QueryOver<AccountabilityNodeUserMap>()
                        .JoinAlias(x => x.AccountabilityNode, () => nodeAlias)
                        .Where(x => x.AccountabilityNodeId == node.Id && x.DeleteTime == null && nodeAlias.DeleteTime == null)
                        .Select(x => x.Id).Take(1).Future<long>();

          a.DeleteTime = null;
          s.Update(a);
          success++;
          count++;
        }
        if (count == 1 && success == 1) {
          messages.Add("Re-added user");
        } else if (success == count) {
          messages.Add("Re-added user to " + success + " seats.");
        } else {
          messages.Add("Re-added user to " + success + "/" + count + " seats.");
        }
        return allSuccess;
      }
      public static List<long> GetSubordinatesAndSelf(UserOrganizationModel caller, long userId, bool includeTempUsers = true) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            return GetSubordinatesAndSelf(s, caller, userId, includeTempUsers: includeTempUsers);
          }
        }
      }
      public static List<UserOrganizationModel> GetSubordinatesAndSelfModels(UserOrganizationModel caller, long userId) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            return GetSubordinatesAndSelfModels(s, caller, userId);
          }
        }
      }

      public static List<UserOrganizationModel> GetSubordinatesAndSelfModels(ISession s, UserOrganizationModel caller, long userId) {
        var userIds = GetSubordinatesAndSelf(s, caller, userId);
        var users = s.QueryOver<UserOrganizationModel>()
          .WhereRestrictionOn(x => x.Id).IsIn(userIds.ToArray())
          .List().ToList();

        return users;
      }

      public static List<long> GetSubordinatesAndSelf(ISession s, UserOrganizationModel caller, long userId, bool includeTempUsers = true) {
        return GetSubordinatesAndSelf(s, PermissionsUtility.Create(s, caller), userId, includeTempUsers: includeTempUsers).ToList();
      }

      [Todo]
      public static IEnumerable<long> GetSubordinatesAndSelf(ISession s, PermissionsUtility perms, long userId, bool includeTempUsers = true) {

        var user = s.Get<UserOrganizationModel>(userId);
        if (user == null || user.DeleteTime != null)
          throw new PermissionsException("User (" + userId + ") does not exist.");

        perms.ViewOrganization(user.Organization.Id);
        var caller = perms.GetCaller();

        //Confirm that caller actually controls UserId
        Unsafe.EnsureCallerManagesUser_Unsafe(s, caller, userId);

        var allNodesForUserQuery = Unsafe.Criterions.SelectNodeIdsGivenUser_Unsafe(userId);
        var childAcIdsWhereUserIsTheParent = Unsafe.Criterions.SelectDeepSubordinateNodeIdsGivenParentNodes_Unsafe(allNodesForUserQuery);
        var childUserIds = Unsafe.Criterions.SelectUserIdsGivenNodes_Unsafe(childAcIdsWhereUserIsTheParent, null);

        IEnumerable<long> res;
        if (!includeTempUsers) {
          res = s.QueryOver<UserOrganizationModel>()
                .WithSubquery.WhereProperty(x => x.Id).In(childUserIds)
                .Where(x => x.User != null)
                .Select(x => x.Id)
                .List<long>();
          if (user.User != null) {
            res = res.Union(user.Id.AsList());
          }
        } else {
          res = Unsafe.GetFutureIds(s, childUserIds).Union(user.Id.AsList());
        }
        return res.Distinct().ToList();
      }

      public static bool HasChildren(ISession s, PermissionsUtility perms, long userId) {
        var nodeIds = AccountabilityAccessor.GetNodeIdsForUser(s, perms, userId);
        var futureVals = new List<IFutureValue<long>>();
        foreach (var nodeId in nodeIds) {
          futureVals.Add(s.QueryOver<DeepAccountability>().Where(x => x.DeleteTime == null && x.Links > 0 && x.ParentId == nodeId && x.ChildId != nodeId).ToRowCountInt64Query().FutureValue<long>());
        }
        foreach (var f in futureVals) {
          if (f.Value > 0)
            return true;
        }
        return false;
      }

      public static List<UserOrganizationModel> GetDirectReportsAndSelfModels(UserOrganizationModel caller, long userId) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var perms = PermissionsUtility.Create(s, caller);
            return GetDirectReportsAndSelfModels(s, perms, userId);
          }
        }
      }
      [Todo]
      public static List<UserOrganizationModel> GetDirectReportsAndSelfModels(ISession s, PermissionsUtility perms, long userId) {
        var myNodeIds = AccountabilityAccessor.GetNodeIdsForUser(s, perms, userId);
        var users = myNodeIds.SelectMany(nodeId => {
          try {
            var res = DeepAccessor.Nodes.GetDirectReportsAndSelf(s, perms, nodeId);
            return res;
          } catch (Exception e) {
            log.Error("DeepAccessor failed", e);
            return new List<AccountabilityNode>();
          }
        }).SelectMany(x => {
          if (myNodeIds.Contains(x.Id)) {
            return x.GetUsers(s).Where(y => y.Id == userId).ToList();
          } else {
            return x.GetUsers(s);
          }
        })
        .ToList();
        return users;


      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="s"></param>
      /// <param name="perms"></param>
      /// <param name="managerUserId"></param>
      /// <param name="subordinateUserId"></param>
      /// <param name="allowDeletedSubordinateUser">for viewing user details page</param>
      /// <returns></returns>
      [Todo]
      public static bool ManagesUser(ISession s, PermissionsUtility perms, long managerUserId, long subordinateUserId, bool allowDeletedSubordinateUser = false) {
        perms.ViewUserOrganization(managerUserId, false).ViewUserOrganization(subordinateUserId, false);
        var m = s.Get<UserOrganizationModel>(managerUserId);
        var sub = s.Get<UserOrganizationModel>(subordinateUserId);
        if (sub == null || (!allowDeletedSubordinateUser && sub.DeleteTime != null))
          throw new PermissionsException("Subordinate user (" + subordinateUserId + ") does not exist.");
        if (m == null || m.DeleteTime != null)
          throw new PermissionsException("Manager user (" + managerUserId + ") does not exist.");

        if (m.IsRadialAdmin)
          return true;
        if (m.ManagingOrganization && m.Organization.Id == sub.Organization.Id)
          return true;


        var managerNodeIds = Unsafe.Criterions.SelectNodeIdsGivenUser_Unsafe(managerUserId);
        var subordinateNodeIds = Unsafe.Criterions.SelectNodeIdsGivenUser_Unsafe(subordinateUserId, allowDeletedSubordinateUser);

        return s.QueryOver<DeepAccountability>()
          .Where(x => x.DeleteTime == null)
          .WithSubquery.WhereProperty(x => x.ParentId).In(managerNodeIds)
          .WithSubquery.WhereProperty(x => x.ChildId).In(subordinateNodeIds)
          .Select(x => x.Id)
          .Take(1).List<long>().Any();


      }

      public static bool ManagesUser(UserOrganizationModel caller, long managerId, long subordinateId) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var perms = PermissionsUtility.Create(s, caller);

            return ManagesUser(s, perms, managerId, subordinateId);
          }
        }
      }

      public static List<AccountabilityNode> GetNodesForUser(UserOrganizationModel caller, long userId) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var perms = PermissionsUtility.Create(s, caller);
            return GetNodesForUser(s, perms, userId);
          }
        }

      }


      [Todo]
      public static List<AccountabilityNode> GetNodesForUser(ISession s, PermissionsUtility perms, long userId) {
        perms.ViewUserOrganization(userId, false);

        var user = s.Get<UserOrganizationModel>(userId);
        if (user == null || user.DeleteTime != null)
          throw new PermissionsException("User (" + userId + ") does not exist.");
        var usersAcNodeIds = Unsafe.Criterions.SelectNodeIdsGivenUser_Unsafe(userId, false);
        return Unsafe.GetAccountabilityNodesByNodeIdCritera_Unsafe(s, usersAcNodeIds, null);



      }
    }
    #endregion

    public class Nodes {

      [Todo]
      public static List<long> GetChildrenAndSelfGivenUserId(ISession s, PermissionsUtility perms, long userId) {

        perms.ViewUserOrganization(userId, false);
        var myNodesQ = Unsafe.Criterions.SelectNodeIdsGivenUser_Unsafe(userId);
        var childNodesQ = Unsafe.Criterions.SelectDeepSubordinateNodeIdsGivenParentNodes_Unsafe(myNodesQ);

        var myNodesF = Unsafe.GetFutureIds(s, myNodesQ);
        var childNodesF = Unsafe.GetFutureIds(s, childNodesQ);

        var output = new List<long>();
        output.AddRange(myNodesF.ToList());
        output.AddRange(childNodesF.ToList());
        output.Distinct().ToList();

        return output;
      }


      public static List<long> GetChildrenAndSelf(UserOrganizationModel caller, long nodeId) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            return GetChildrenAndSelf(s, caller, nodeId);
          }
        }
      }
      [Todo]
      public static List<long> GetChildrenAndSelf(ISession s, UserOrganizationModel caller, long nodeId) {
        var node = s.Get<AccountabilityNode>(nodeId);

        Unsafe.EnsureCallerManagesNode_Unsafe(s, caller, nodeId);

        var allPermitted = new List<long>() { nodeId };
        var subordinates = s.QueryOver<DeepAccountability>()
                    .Where(x => x.DeleteTime == null)
                    .WhereRestrictionOn(x => x.ParentId).IsIn(allPermitted)
                    .Select(x => x.ChildId)
                    .List<long>()
                    .ToList();

        subordinates.Add(nodeId);
        return subordinates.Distinct().ToList();
      }

      [Obsolete("Can be dangerous, includes all users attached to parent node")]
      public static List<AccountabilityNode> GetChildrenAndSelfModels(ISession s, UserOrganizationModel caller, long nodeId, bool allowAnyFromSameOrg = false) {
        var node = s.Get<AccountabilityNode>(nodeId);

        if (node == null || node.DeleteTime != null) {
          throw new PermissionsException("You don't have access to this user");
        }

        if (allowAnyFromSameOrg && node.OrganizationId == caller.Organization.Id) {
          //^ this is the permission check ^
        } else {
          Unsafe.EnsureCallerManagesNode_Unsafe(s, caller, nodeId);
        }



        var acNodeIds = Unsafe.Criterions.SelectDeepSubordinateNodeIdsGivenParentNode_Unsafe(nodeId);
        List<AccountabilityNode> subordinates = Unsafe.GetAccountabilityNodesByNodeIdCritera_Unsafe(s, acNodeIds, nodeId);


        return subordinates;
      }
      public static List<AccountabilityNode> GetDirectReportsAndSelf(UserOrganizationModel caller, long forNodeId) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var perms = PermissionsUtility.Create(s, caller);
            return GetDirectReportsAndSelf(s, perms, forNodeId);
          }
        }
      }
      [Todo]
      public static List<AccountabilityNode> GetDirectReportsAndSelf(ISession s, PermissionsUtility perms, long forNodeId) {
        var forNode = s.Get<AccountabilityNode>(forNodeId);
        perms.ViewHierarchy(forNode.AccountabilityChartId);

        var childrenAndSelfNodeIds = Unsafe.Criterions.SelectDirectChildNodeIdsGivenParentNode_Unsafe(forNodeId);
        var directReportsAndSelf = Unsafe.GetAccountabilityNodesByNodeIdCritera_Unsafe(s, childrenAndSelfNodeIds, forNodeId);

        return directReportsAndSelf;







      }

      public static List<AccountabilityNode> GetNodesForOrganization(ISession s, PermissionsUtility perms, long orgId) {
        var org = s.Get<OrganizationModel>(orgId);
        perms.ViewHierarchy(org.AccountabilityChartId);
        var chart = s.Get<AccountabilityChart>(org.AccountabilityChartId);
        return GetChildrenAndSelfModels(s, perms.GetCaller(), chart.RootId, true);
      }






    }

    public class Permissions {

      public static bool HasChildren(ISession s, long nodeId) {
        return s.QueryOver<DeepAccountability>()
              .Where(x => x.DeleteTime == null && x.Links > 0 && x.ParentId == nodeId && x.ChildId != nodeId)
              .Take(1).RowCount() > 0;
      }

      [Todo]
      public static bool ManagesNode(ISession s, PermissionsUtility perms, long managerUserId, long nodeId) {
        perms.ViewUserOrganization(managerUserId, false);
        var m = s.Get<UserOrganizationModel>(managerUserId);
        var node = s.Get<AccountabilityNode>(nodeId);

        var user = s.Get<UserOrganizationModel>(managerUserId);
        if (user == null || user.DeleteTime != null)
          throw new PermissionsException("User (" + managerUserId + ") does not exist.");

        if (m.IsRadialAdmin)
          return true;
        if (m.ManagingOrganization && m.Organization.Id == node.OrganizationId)
          return true;

        if (node == null || node.DeleteTime != null)
          throw new PermissionsException("Accountability node (" + nodeId + ") does not exist.");


        var managerNodeIds = Unsafe.Criterions.SelectNodeIdsGivenUser_Unsafe(managerUserId);

        return s.QueryOver<DeepAccountability>()
          .Where(x => x.DeleteTime == null && x.ChildId == nodeId)
          .WithSubquery.WhereProperty(x => x.ParentId).In(managerNodeIds)
          .Select(x => x.Id)
          .Take(1).List<long>().Any();




      }

    }

    public class Maps {
      public static List<DeepAccountability> GetOrganizationMap(ISession s, PermissionsUtility perm, long organizationId) {
        perm.ViewOrganization(organizationId);
        return s.QueryOver<DeepAccountability>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId).List().ToList();
      }
    }

    public class Unsafe {
      public class Actions {


        [Obsolete("Use UserAccessor.AddManager", false)]
        public static void Add(ISession s, AccountabilityNode manager, AccountabilityNode subordinate, long organizationId, DateTime now, bool ignoreCircular = false) {
          //Get **users** subordinates, make them deep subordinates of manager
          var allSubordinates = Dive.GetSubordinates(s, subordinate);
          allSubordinates.Add(subordinate);
          var allSuperiors = Dive.GetSuperiors(s, manager);
          allSuperiors.Add(manager);

          var allManagerSubordinates = s.QueryOver<DeepAccountability>()
            .Where(x => x.DeleteTime == null)
            .WhereRestrictionOn(x => x.ParentId).IsIn(allSuperiors.Select(x => x.Id).ToList())
            .List().ToList();


          //for manager and each of his superiors
          foreach (var SUP in allSuperiors) {
            var managerSubordinates = allManagerSubordinates.Where(x => x.ParentId == SUP.Id).ToList();

            foreach (var sub in allSubordinates) {
              if (sub.Id == SUP.Id && !ignoreCircular) {
                var mname = "" + manager.Id;
                var sname = "" + subordinate.Id;

                throw new PermissionsException("A circular dependency was found. " + mname + " cannot manage " + sname + " because " + mname + " is " + sname + "'s subordinate.");
              }

              var found = managerSubordinates.FirstOrDefault(x => x.ChildId == sub.Id);
              if (found == null) {
                found = new DeepAccountability() {
                  CreateTime = now,
                  ParentId = SUP.Id,
                  Parent = s.Load<AccountabilityNode>(SUP.Id),
                  ChildId = sub.Id,
                  Child = s.Load<AccountabilityNode>(sub.Id),
                  Links = 0,
                  OrganizationId = organizationId
                };
              }
              found.Links += 1;
              s.SaveOrUpdate(found);
            }
          }
        }

        [Obsolete("Use UserAccessor.RemoveManager", false)]
        public static void Remove(ISession s, AccountabilityNode parent, AccountabilityNode child, DateTime now, bool ignoreCircular = false) {
          //Grab all subordinates' deep subordinates
          var allSuperiors = Dive.GetSuperiors(s, parent);
          allSuperiors.Add(parent);
          var allSubordinates = Dive.GetSubordinates(s, child);
          allSubordinates.Add(child);


          foreach (var SUP in allSuperiors) {
            var managerSubordinates = s.QueryOver<DeepAccountability>().Where(x => x.ParentId == SUP.Id).List().ToListAlive();

            foreach (var sub in allSubordinates) {
              var found = managerSubordinates.FirstOrDefault(x => x.ChildId == sub.Id && x.Links > 0);
              if (found == null) {
                log.Error("Manager link doesn't exist for orgId=(" + parent.OrganizationId + "). Advise that you run deep subordinate creation.");
              } else {
                found.Links -= 1;
                if (found.Links == 0)
                  found.DeleteTime = now;
                if (found.Links < 0) {
                  if (!ignoreCircular)
                    throw new Exception("This shouldn't happen.");
                }
                s.Update(found);
              }
            }
          }
        }

        public static void RemoveAll(ISession s, AccountabilityNode node, DateTime now) {
          var id = node.Id;
          var all = s.QueryOver<DeepAccountability>().Where(x => (x.ParentId == id || x.ChildId == id) && x.DeleteTime == null).List().ToList();

          foreach (var a in all) {
            a.DeleteTime = now;
            s.Update(a);
          }

        }

      }


      public class Criterions {

        /// <summary>
        /// Gets the node ids for a given user
        /// Confirms the user, node, and link are alive
        /// </summary>
        public static QueryOver<AccountabilityNodeUserMap, AccountabilityNodeUserMap> SelectNodeIdsGivenUser_Unsafe(long userId, bool allowDeletedUsers = false) {
          UserOrganizationModel userAlias = null;
          AccountabilityNode nodeAlias1 = null;
          var query = QueryOver.Of<AccountabilityNodeUserMap>()
              .JoinAlias(x => x.User, () => userAlias)
              .JoinAlias(x => x.AccountabilityNode, () => nodeAlias1)
              .Where(x => x.UserId == userId);  //Matches user

          if (allowDeletedUsers) {
            query = query.Where(x =>
                x.DeleteTime == null &&       //Map alive      
                nodeAlias1.DeleteTime == null);   //Node alive   
          } else {
            query = query.Where(x =>
                x.DeleteTime == null &&        //Map alive     
                nodeAlias1.DeleteTime == null &&     //Node alive
                userAlias.DeleteTime == null);       //User alive
          }
          return query.Select(Projections.Distinct(Projections.Cast(NHibernateUtil.Int64, Projections.Property<AccountabilityNodeUserMap>(x => x.AccountabilityNodeId))));
        }

        /// <summary>
        /// Gets the (alive) node ids for the direct reports of a particular node
        /// </summary>
        public static QueryOver<AccountabilityNode, AccountabilityNode> SelectDirectChildNodeIdsGivenParentNode_Unsafe(long parentNodeId) {
          var builder = QueryOver.Of<AccountabilityNode>().Where(x => x.DeleteTime == null);
          builder = builder.Where(x => x.ParentNodeId == parentNodeId);
          return builder.Select(Projections.Distinct(
            Projections.Cast(NHibernateUtil.Int64, Projections.Property<AccountabilityNode>(x => x.Id))
           )
         );
        }

        public static QueryOver<DeepAccountability, DeepAccountability> SelectDeepSubordinateNodeIdsGivenParentNode_Unsafe(long parentId) {


          AccountabilityNode nodeAlias2 = null;
          return QueryOver.Of<DeepAccountability>()
                .JoinAlias(x => x.Child, () => nodeAlias2)
                .Where(x => x.DeleteTime == null && nodeAlias2.DeleteTime == null && x.ParentId == parentId)
                .Select(Projections.Distinct(Projections.Cast(NHibernateUtil.Int64, Projections.Property<DeepAccountability>(x => x.ChildId))));
        }

        /// <summary>
        /// Gets the (alive) node ids for the direct reports of a particular node
        /// </summary>
        public static QueryOver<DeepAccountability, DeepAccountability> SelectDeepSubordinateNodeIdsGivenParentNodes_Unsafe<U>(QueryOver<U> parentAcNodeIds) {


          AccountabilityNode nodeAlias2 = null;
          return QueryOver.Of<DeepAccountability>()
                .JoinAlias(x => x.Child, () => nodeAlias2)
                .Where(x => x.DeleteTime == null && nodeAlias2.DeleteTime == null)
                .WithSubquery.WhereProperty(x => x.ParentId).In(parentAcNodeIds)
                .Select(Projections.Distinct(Projections.Cast(NHibernateUtil.Int64, Projections.Property<DeepAccountability>(x => x.ChildId))));
        }







        /// <summary>
        /// Get user ids for a set of node ids
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="nodeIds"></param>
        /// <param name="alsoIncludeNodeId"></param>
        /// <returns></returns>
        public static QueryOver<AccountabilityNodeUserMap, AccountabilityNodeUserMap> SelectUserIdsGivenNodes_Unsafe<U>(QueryOver<U> nodeIds, long? alsoIncludeNodeId) {

          var ors = Restrictions.Disjunction().Add(Subqueries.WhereProperty<AccountabilityNodeUserMap>(x => x.AccountabilityNodeId).In(nodeIds));
          if (alsoIncludeNodeId != null) {
            ors = ors.Add(Restrictions.Eq(Projections.Property<AccountabilityNodeUserMap>(x => x.AccountabilityNodeId), alsoIncludeNodeId.Value));
          }

          UserOrganizationModel userAlias = null;
          AccountabilityNode nodeAlias1 = null;
          return QueryOver.Of<AccountabilityNodeUserMap>()
              .JoinAlias(x => x.User, () => userAlias)
              .JoinAlias(x => x.AccountabilityNode, () => nodeAlias1)
              .Where(x =>
                x.DeleteTime == null &&             //Map alive
                nodeAlias1.DeleteTime == null &&    //Node alive
                userAlias.DeleteTime == null        //User alive
              )
              .Where(ors)
              .Select(
                Projections.Distinct(
                  Projections.Cast(NHibernateUtil.Int64, Projections.Property<AccountabilityNodeUserMap>(x => x.UserId))
                )
              );
        }
      }

      public static IEnumerable<long> GetFutureIds<U>(ISession s, QueryOver<U> idQuery) {
        return idQuery.GetExecutableQueryOver(s).Future<long>();
      }


      public static bool IsUserAttachedToNode_Unsafe(ISession s, long userId, long nodeId) {
        AccountabilityNode nodeAlias = null;
        UserOrganizationModel userAlias = null;
        return s.QueryOver<AccountabilityNodeUserMap>()
              .JoinAlias(x => x.User, () => userAlias)
              .JoinAlias(x => x.AccountabilityNode, () => nodeAlias)
              .Where(x =>
                x.DeleteTime == null &&      //Not deleted
                userAlias.DeleteTime == null && //Not deleted
                nodeAlias.DeleteTime == null && //Not deleted
                x.UserId == userId &&   //User Id
                x.AccountabilityNodeId == nodeId    //Accountability node Id
              ).Select(x => x.Id)
              .Take(1)
              .List<long>().ToList().Count != 0;
      }


      public static void EnsureCallerManagesNode_Unsafe(ISession s, UserOrganizationModel caller, long nodeId) {
        AccountabilityNode nodeAlias = null;
        UserOrganizationModel userAlias = null;

        //caller was an admin
        if (PermissionsUtility.IsAdmin(s, caller))
          return;


        var isCaller = IsUserAttachedToNode_Unsafe(s, caller.Id, nodeId);

        if (isCaller)
          return;

        var node = s.Get<AccountabilityNode>(nodeId);
        if (node.DeleteTime != null) {
          throw new PermissionsException("You don't have access to this user");
        }

        var callerNodeIds = Criterions.SelectNodeIdsGivenUser_Unsafe(caller.Id);

        AccountabilityNode parent = null;
        var found = s.QueryOver<DeepAccountability>()
                .Where(x => x.DeleteTime == null && x.ChildId == nodeId)/*Child node delete time tested above*/
                .JoinAlias(x => x.Parent, () => parent)
                .Where(x => parent.DeleteTime == null)
                .WithSubquery.WhereProperty(x => x.ParentId).In(callerNodeIds)
                .Take(1)
                .List()
                .SingleOrDefault();

        if (found == null)
          throw new PermissionsException("You don't have access to this user");
      }


      public static void EnsureCallerManagesUser_Unsafe(ISession s, UserOrganizationModel caller, long userId) {
        //caller is user
        if (caller.Id == userId)
          return;
        //caller was an admin
        if (PermissionsUtility.IsAdmin(s, caller))
          return;

        var parentNodeIds = Criterions.SelectNodeIdsGivenUser_Unsafe(caller.Id);
        var childNodeIds = Criterions.SelectNodeIdsGivenUser_Unsafe(userId);

        var found = s.QueryOver<DeepAccountability>()
          .Where(x => x.DeleteTime == null)
          .WithSubquery.WhereProperty(x => x.Parent.Id).In(parentNodeIds)
          .WithSubquery.WhereProperty(x => x.Child.Id).In(childNodeIds)
          .Select(x => x.Id).Take(1)
          .List<long>();
        if (found.Count() == 0) {
          throw new PermissionsException("You don't have access to this user");
        }
      }






      public static List<AccountabilityNode> GetAccountabilityNodesByNodeIdCritera_Unsafe<U>(ISession s, QueryOver<U> acNodeIds, long? alsoIncludeNodeId) {

        //In supplied list of nodes.

        var allAccNodesOrs = Restrictions.Disjunction().Add(Subqueries.WhereProperty<AccountabilityNode>(x => x.Id).In(acNodeIds));
        if (alsoIncludeNodeId != null) {
          allAccNodesOrs = allAccNodesOrs.Add(Restrictions.Eq(Projections.Property<AccountabilityNode>(x => x.Id), alsoIncludeNodeId.Value));
        }


        AccountabilityRolesGroup argAlias = null;
        var allAccNodesQ = s.QueryOver<AccountabilityNode>()
          .Where(allAccNodesOrs)
          .JoinAlias(x => x.AccountabilityRolesGroup, () => argAlias, JoinType.LeftOuterJoin)
          .Future();



        var allMapsOrs = Restrictions.Disjunction().Add(Subqueries.WhereProperty<AccountabilityNodeUserMap>(x => x.AccountabilityNodeId).In(acNodeIds));
        if (alsoIncludeNodeId != null) {
          allMapsOrs = allMapsOrs.Add(Restrictions.Eq(Projections.Property<AccountabilityNodeUserMap>(x => x.AccountabilityNodeId), alsoIncludeNodeId.Value));
        }

        var allMapsQ = s.QueryOver<AccountabilityNodeUserMap>()
          .Where(x => x.DeleteTime == null)
          .Where(allMapsOrs)
          .Future();


        TempUserModel tempUserAlias = null;
        UserLookup userLookupAlias = null;


        ForceIndexInterceptor.SetForceIndexHint(s, "AccountabilityNodeUserMap");
        var childUserIds = Criterions.SelectUserIdsGivenNodes_Unsafe(acNodeIds, alsoIncludeNodeId)
          .GetExecutableQueryOver(s)
          .Future<long>()
          .ToArray();
        //Subquery failed to optimize in mysql runtime. separating out seems to be faster unfortunately.
        //var childUserIds = Criterions.SelectUserIdsGivenNodes_Unsafe(acNodeIds, alsoIncludeNodeId);

        var uomQuery = s.QueryOver<UserOrganizationModel>();


        // -----This is a hack that edits the HQL-----
        // It is intended to insert a "FORCE INDEX (PRIMARY)" into the code to improve performance
        // It produces a 5-10x speed improvement 
        // NHibernate does not have this command natively.

        ForceIndexInterceptor.SetForceIndexHint(s, "UserOrganizationModel");
        //ForceIndexInterceptor.SetForceIndexHint(s, "AccountabilityNodeUserMap");

        var allUsersQ = uomQuery
          .Where(x => x.DeleteTime == null)
          .JoinAlias(x => x.Cache, () => userLookupAlias, JoinType.LeftOuterJoin)
          .JoinAlias(x => x.TempUser, () => tempUserAlias, JoinType.LeftOuterJoin)
          .WhereRestrictionOn(x => x.Id).IsIn(childUserIds)
          //.WithSubquery.WhereProperty(x => x.Id).In(childUserIds)
          .Future();

        //inject users
        var allAccNodes = allAccNodesQ.Distinct(x => x.Id).ToList();
        var allUsers = allUsersQ.ToList();

        var allMaps = allMapsQ.ToList();
        var acNodePopulated = allAccNodes.ToList();

        //Fix me, we're passing in deleted users.

        acNodePopulated.ForEach(x => {
          x.SetUsers(allMaps, allUsers);
        });

        return acNodePopulated;
      }

    }
  }
}
