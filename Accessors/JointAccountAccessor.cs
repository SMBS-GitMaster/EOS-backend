using DocumentFormat.OpenXml.EMMA;
using FluentNHibernate.Testing.Values;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.Accessors.PaymentAccessor;

namespace RadialReview.Core.Accessors {

  public class ChildAccountData {
    public long OrgId { get; set; }
    public string Name { get; set; }
    public long PayingUsers { get; set; }
    public decimal EstimatedPayment { get; set; }
    public bool CallerIsPaying { get; set; }
    public bool ParentIsPaying { get; set; }
    public string ParentOrgName { get; set; }
    public long? ParentOrgId { get; set; }
    public int Depth { get; set; }
    public List<ItemizedCharge.UserCharge> Users { get; set; }
  }

  public class JointAccountAccessor {

    public static List<ChildAccountData> GetOwnedOrgs(UserOrganizationModel caller, long orgId, bool payingOnly) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);

          var orgLinks = GetOwnedOrgs(s, perms, orgId, payingOnly);
          var orgIds = orgLinks.Select(x => x.ChildOrgId).Distinct().ToList();
          var orgs = s.QueryOver<OrganizationModel>()
                        .WhereRestrictionOn(x => x.Id).IsIn(orgIds)
                        .List().ToList();
          var orgLookup = orgs.ToDefaultDictionary(x => x.Id, x => x, x => null);


          var now = DateTime.UtcNow;
          var result = new List<ChildAccountData>();
          foreach (var ol in orgLinks.DepthFirst(orgId)) {

            var o = orgLookup[ol.Link.ChildOrgId];

            decimal charge = 0.0m;
            int paidUserCount = 0;
            var callerIsPaying = orgLinks.DoesParentPaysForChild(orgId, o.Id);
            var parentId = orgLinks.GetParentId(o.Id);
            var link = orgLinks.GetLink(parentId??0, o.Id);
            var parentName = orgLookup[parentId??0].NotNull(x => x.GetName());

            var users = new List<ItemizedCharge.UserCharge>();

            if (callerIsPaying) {
              var maxDate = Math2.Max(now, o.PaymentPlan.FreeUntil.AddSeconds(1));
              var dedup = new Dedup();

              var usersAndPlan = PaymentAccessor.GetUsersAndPlanForOrganization_Unsafe(s, o.Id, maxDate);

              var itemized = PaymentAccessor.CalculateChargeLessTaxAndCreditsPerOrganization(usersAndPlan, dedup);// s, o, o.PaymentPlan, maxDate, dedup);
              charge = itemized.CalculateTotal();
              paidUserCount = itemized.ChargedFor.Count();
              users = itemized.ChargedFor.ToList();
            }

            var cad = new ChildAccountData() {
              OrgId = o.Id,
              Name = o.Name,
              EstimatedPayment =charge,
              PayingUsers = paidUserCount,
              CallerIsPaying = callerIsPaying,
              ParentIsPaying =link.NotNull(x => x.Paying),
              ParentOrgName = parentName,
              ParentOrgId = parentId,
              Depth = ol.Depth,
              Users = users,

            };
            result.Add(cad);
          }



          //Do not save here.
          return result;
        }
      }
    }

    public static List<long> GetOwnedOrgIds(UserOrganizationModel caller, long orgId, bool payingOnly) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetOwnedOrgs(s, perms, orgId, payingOnly).Select(x => x.ChildOrgId).ToList();
        }
      }
    }

    public class LinkPayingDepth {
      public LinkPaying Link { get; set; }
      public int Depth { get; set; }
    }

    public class LinkPaying {
      public long ParentOrgId { get; set; }
      public long ChildOrgId { get; set; }
      public bool Paying { get; set; }
    }

    public class OwnedOrgTree : IEnumerable<LinkPaying> {
      public OwnedOrgTree(List<LinkPaying> links) {
        Links=links;

        SourceDest = new DefaultDictionary<long, List<LinkPaying>>(x => new List<LinkPaying>());
        DestSource = new DefaultDictionary<long, long?>(x => null);
        foreach (var link in Links) {
          if (!SourceDest.ContainsKey(link.ParentOrgId))
            SourceDest[link.ParentOrgId] = new List<LinkPaying>();
          SourceDest[link.ParentOrgId].Add(link);
          DestSource[link.ChildOrgId] = link.ParentOrgId;
        }

      }

      private List<LinkPaying> Links { get; set; }
      private DefaultDictionary<long, List<LinkPaying>> SourceDest { get; set; }
      private DefaultDictionary<long, long?> DestSource { get; set; }

      public List<LinkPaying> GetDirectChildren(long parentId) {
        return SourceDest[parentId];
      }

      public long? GetParentId(long childId) {
        return DestSource[childId];
      }

      public bool DoesParentPaysForChild(long parentId, long childId) {
        return DepthFirst_Recursive(parentId, (pid, link) => {
          if (link.Paying) {
            if (link.ChildOrgId == childId) {
              //Found it
              return PredicateResult.YieldTrue;
            }
          } else {
            //wasnt paying.. no children count
            return PredicateResult.SkipSubtree;
          }
          //not it continue..
          return PredicateResult.YieldFalse;
        });
      }

      public enum PredicateResult {
        YieldTrue,
        YieldFalse,
        SkipSubtree
      }

      private bool DepthFirst_Recursive(long parentId, Func<long, LinkPaying, PredicateResult> predicate, HashSet<long> seenParent = null) {
        seenParent = seenParent ?? new HashSet<long>();

        //Short circuit
        if (seenParent.Contains(parentId))
          return false;
        seenParent.Add(parentId);

        foreach (var c in SourceDest[parentId]) {
          var res = predicate(parentId, c);
          switch (res) {
            case PredicateResult.YieldTrue:
              return true;
            case PredicateResult.YieldFalse:
              break;
            case PredicateResult.SkipSubtree:
              continue;
            default:
              throw new NotImplementedException();
          }
          var childrenRes = DepthFirst_Recursive(c.ChildOrgId, predicate, seenParent);
          if (childrenRes)
            return true;
        }
        return false;
      }

      public IEnumerator<LinkPaying> GetEnumerator() {
        return Links.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator() {
        return Links.GetEnumerator();
      }

      public LinkPaying GetLink(long parentId, long childId) {
        return Links.SingleOrDefault(x => x.ParentOrgId == parentId && x.ChildOrgId == childId);
      }

      public IEnumerable<LinkPayingDepth> DepthFirst(long parentId, int depth = 0, HashSet<long> seen = null) {
        seen = seen ?? new HashSet<long>();
        if (seen.Contains(parentId)) {
          yield break;
        }
        seen.Add(parentId);

        var children = SourceDest[parentId];
        foreach (var c in children) {
          yield return new LinkPayingDepth() {
            Depth = depth,
            Link = c
          };
          foreach (var cc in DepthFirst(c.ChildOrgId, depth+1, seen)) {
            yield return cc;
          }

        }
      }
    }


    public static OwnedOrgTree GetOwnedOrgs(ISession s, PermissionsUtility perms, long parentOrgId, bool payingOnly) {
      //perms.ManagingOrganization(parentOrgId)
      perms.Or(x => x.ManagingOrganization(parentOrgId), x => x.CanView(PermItem.ResourceType.UpdatePaymentForOrganization, parentOrgId));
      return GetOwnedOrgs_Unsafe(s, parentOrgId, payingOnly);
    }

    public static OwnedOrgTree GetOwnedOrgs_Unsafe(ISession s, long parentOrgId, bool payingOnly) {
      OrganizationModel parentOrgAlias = null;
      var parent = s.QueryOver<JointOrganizationModel>()
            .JoinAlias(x => x.ParentOrg, () => parentOrgAlias)
            .Where(x => x.DeleteTime==null && x.ChildOrgId == parentOrgId)
            .Select(x => x.ParentPays, x => parentOrgAlias.DeleteTime)
            .List<object[]>().Select(x => new {
              ParentPays = (bool)x[0],
              ParentOrgDeleteTime = (DateTime?)x[1]
            }).SingleOrDefault();

      //Return nothing if it's parent is paying.
      var payingForSelf = parent==null || parent.ParentOrgDeleteTime != null || !parent.ParentPays;
      if (payingOnly && !payingForSelf) {
        return new OwnedOrgTree(new List<LinkPaying>());
      }


      var seen = new HashSet<long>();
      //Add self....
      var results = new List<LinkPaying>() {
        new LinkPaying(){
          ChildOrgId = parentOrgId,
          ParentOrgId = parentOrgId,
          Paying = payingForSelf
        }
      };

      bool? payingOverriddenTo = null;
      if (!payingForSelf) {
        payingOverriddenTo  = false;
      }


      if (payingOnly && payingOverriddenTo == false) {
        //skip it
      } else {
        //Add children...
        var childrenResults = GetOwnedOrgsRecursive_Unsafe(s, parentOrgId, seen, payingOnly, payingOverriddenTo);
        results.AddRange(childrenResults);
      }
      return new OwnedOrgTree(results);
    }

    private static List<LinkPaying> GetOwnedOrgsRecursive_Unsafe(ISession s, long parentOrgId, HashSet<long> seen, bool payingOnly, bool? payingOverridenTo) {
      //Add self
      var result = new List<LinkPaying>();

      OrganizationModel childOrgAlias = null;
      var childrenQ = s.QueryOver<JointOrganizationModel>()
            .JoinAlias(x => x.ChildOrg, () => childOrgAlias)
            .Where(x => x.DeleteTime==null && x.ParentOrgId == parentOrgId && childOrgAlias.DeleteTime == null);

      if (payingOnly) {
        childrenQ = childrenQ.Where(x => x.ParentPays);
      }

      var children = childrenQ.List().ToList();

      foreach (var oc in children) {
        if (seen.Contains(oc.ChildOrgId))
          continue;//loop detected.
        seen.Add(oc.ChildOrgId);
        result.Add(new LinkPaying() {
          ParentOrgId = parentOrgId,
          ChildOrgId = oc.ChildOrgId,
          Paying = payingOverridenTo ?? oc.ParentPays
        });
        result.AddRange(GetOwnedOrgsRecursive_Unsafe(s, oc.ChildOrgId, seen, payingOnly, payingOverridenTo));
      }
      return result;
    }

    public static List<NameId> GetChildAccounts(UserOrganizationModel caller, long parentId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewOrganization(parentId);


          var nameIds = s.QueryOver<JointOrganizationModel>()
                      .Where(x => x.DeleteTime == null && x.ParentOrgId == parentId)
                      .Select(x => x.ChildOrgId, x => x.ChildOrg.Name)
                      .List<object[]>()
                      .Select(x => new NameId((string)x[1], (long)x[0]))
                      .ToList();
          return nameIds;
        }
      }
    }

    public static long? GetParentOrgId(UserOrganizationModel caller, long orgId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetParentOrgId(s, perms, orgId);

        }
      }
    }

    public static long? GetParentOrgId(ISession s, PermissionsUtility perms, long orgId) {
      perms.ViewOrganization(orgId);
      return s.QueryOver<JointOrganizationModel>()
                  .Where(x => x.DeleteTime == null && x.ChildOrgId == orgId)
                  .Select(x => x.ParentOrgId)
                  .SingleOrDefault<long?>();
    }

    public static async Task<bool> SetPaying(UserOrganizationModel caller, long parentOrgId, long childOrgId, bool parentPaying) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var res = await SetPaying(s, perms, parentOrgId, childOrgId, parentPaying);
          tx.Commit();
          s.Flush();
          return res;
        }
      }
    }

    public static async Task<bool> SetPaying(ISession s, PermissionsUtility perms, long parentOrgId, long childOrgId, bool parentPaying) {
      perms.ManagingOrganization(parentOrgId);
      perms.RadialAdmin();

      var found = s.QueryOver<JointOrganizationModel>()
        .Where(x => x.ParentOrgId == parentOrgId && x.ChildOrgId == childOrgId && x.DeleteTime==null)
        .List().ToList().SingleOrDefault();

      if (found ==null)
        throw new PermissionsException("Joint Account Link does not exists");

      if (found.ParentPays == parentPaying)
        return false;

      var oldStatus = found.ParentPays;
      found.ParentPays = parentPaying;
      s.Update(found);

      await HooksRegistry.Each<IJointOrganizationHook>((ses, x) => x.UpdatePaying(ses, found, new IJointOrganizationHookUpdatePayingUpdates() {
        ChildOrganizationId = childOrgId,
        ParentOrganizationId = parentOrgId,
        OldParentPayingStatus = oldStatus,
        NewParentPayingStatus = false,
        EventType = UpdatePayingEventType.OnPayingUpdated
      }));
      return true;

    }


    /// <summary>
    /// returns whether an update has happened.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="parentId"></param>
    /// <param name="childOrgId"></param>
    /// <param name="parentPaying"></param>
    /// <returns></returns>
    public static async Task<bool> SetParent(UserOrganizationModel caller, long? parentId, long childOrgId, bool? parentPaying) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.RadialAdmin()
            .ManagingOrganization(childOrgId);

          var now = DateTime.UtcNow;

          //Confirm that both child and parent have the same payment period in order to link them. Otherwise they could be charged multiple times in a period.
          if (parentId !=null) {
            var p = s.Get<OrganizationModel>(parentId.Value);
            var c = s.Get<OrganizationModel>(childOrgId);
            if (p.PaymentPlan is PaymentPlan_Monthly ppm && c.PaymentPlan is PaymentPlan_Monthly cpm && (ppm.SchedulePeriod ?? SchedulePeriodType.Monthly) != (cpm.SchedulePeriod ?? SchedulePeriodType.Monthly)) {
              throw new PermissionsException("The payment plan period for the parent and child must be the same. Parent:"+(ppm.SchedulePeriod ?? SchedulePeriodType.Monthly)+", Child:"+ (cpm.SchedulePeriod ?? SchedulePeriodType.Monthly));
            }
          }


          //Scope "existing" to here only to prevent bugs
          {
            var existing = s.QueryOver<JointOrganizationModel>()
              .Where(x => x.ChildOrgId==childOrgId && x.DeleteTime==null)
              .Take(1)
              .SingleOrDefault();

            //No changes
            if (existing != null && parentId!=null && existing.ParentOrgId==parentId) {
              if (parentPaying == null || existing.ParentPays == parentPaying) {
                //already set.
                return false;
              } else {
                //just updating paying
                existing.ParentPays = parentPaying.Value;
                s.Update(existing);
                tx.Commit();
                s.Flush();
                return true;
              }
            }


            //Remove Existing Parent
            if (existing != null) {
              existing.DeleteTime = now;
              s.Update(existing);
              //Remove Parent
              await HooksRegistry.Each<IJointOrganizationHook>((ses, x) => x.RemoveParent(ses, existing, new IJointOrganizationHookRemoveUpdates() {
                ChildOrganizationId = childOrgId,
                OldParentOrganizationId = existing.ParentOrgId
              }));
              //Unset Paying
              await HooksRegistry.Each<IJointOrganizationHook>((ses, x) => x.UpdatePaying(ses, existing, new IJointOrganizationHookUpdatePayingUpdates() {
                ChildOrganizationId = childOrgId,
                ParentOrganizationId = existing.ParentOrgId,
                OldParentPayingStatus = existing.ParentPays,
                NewParentPayingStatus = false,
                EventType = UpdatePayingEventType.OnLinkRemoved
              }));
            }
          }

          //Now add new parent
          if (parentId !=null) {
            perms.ManagingOrganization(parentId.Value);
            //Esure no parent exists
            var added = new JointOrganizationModel() {
              ChildOrgId = childOrgId,
              ParentOrgId = parentId.Value,
              ChildOrg = s.Load<OrganizationModel>(childOrgId),
              ParentOrg = s.Load<OrganizationModel>(parentId.Value),
              CreateTime = now,
              ParentPays = parentPaying ?? true,
            };
            s.Save(added);
            //Add Parent
            await HooksRegistry.Each<IJointOrganizationHook>((ses, x) => x.AddParent(ses, added, new IJointOrganizationHookAddUpdates() {
              ChildOrganizationId = childOrgId,
              NewParentOrganizationId = added.ParentOrgId
            }));
            //Update Paying
            await HooksRegistry.Each<IJointOrganizationHook>((ses, x) => x.UpdatePaying(ses, added, new IJointOrganizationHookUpdatePayingUpdates() {
              ChildOrganizationId = childOrgId,
              ParentOrganizationId = added.ParentOrgId,
              OldParentPayingStatus = null,
              NewParentPayingStatus = added.ParentPays,
              EventType = UpdatePayingEventType.OnCreate
            }));
          }


          tx.Commit();
          s.Flush();
          return true;
        }
      }
    }


  }
}
