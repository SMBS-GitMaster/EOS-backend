using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentResults;
using Hangfire;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Core.Accessors;
using RadialReview.Core.Accessors.StrictlyAfterExecutors;
using RadialReview.Core.Models.Terms;
using RadialReview.Core.Repositories;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.Hangfire;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.Askables;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelIssue = RadialReview.GraphQL.Models.IssueQueryModel;

namespace RadialReview.Accessors {
  public class VtoAccessor : BaseAccessor {

    public static async Task UpdateAllVTOs(ISession s, long organizationId, string connectionId, Action<RealTimeUtility.GroupUpdater> action) {
      await using (var rt = RealTimeUtility.Create(connectionId)) {
        var vtoIds = s.QueryOver<VtoModel>().Where(x => x.Organization.Id == organizationId).Select(x => x.Id).List<long>();
        foreach (var vtoId in vtoIds) {
          var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateVtoGroupId(vtoId));
          action(group);
        }
      }
    }

    public static async Task UpdateVTO(long vtoId, string connectionId, Action<RealTimeUtility.GroupUpdater> action) {
      await using (var rt = RealTimeUtility.Create(connectionId)) {
        var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateVtoGroupId(vtoId));
        action(group);
      }
    }

    private static Random Rand = new Random();

    private static void MarkUpdated(long vtoId) {
      var now = DateTime.UtcNow;
      //get 15+rnd*15 second horizon
      var offsetSec = 10+Rand.NextDouble()*15;
      Scheduler.Schedule(() => MarkUpdatedVto_Hangfire(vtoId, now), DateTimeOffset.UtcNow.AddSeconds(offsetSec));
    }

    private static void MarkUpdated(VtoModel vto) {
      if (vto!=null) {
        MarkUpdated(vto.Id);
      }
    }

    [Queue(HangfireQueues.Immediate.MARK_VTO_UPDATED)]
    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]

    public static int MarkUpdatedVto_Hangfire(long vtoId, DateTime now) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var q = s.CreateQuery("update VtoModel v set v.LastModified=:now where v.Id=:id and v.LastModified < :now")
            .SetDateTime("now", now)
            .SetParameter("id", vtoId);
          var rows = q.ExecuteUpdate();
          tx.Commit();
          s.Flush();
          return rows;
        }
      }
    }

    public static List<VtoModel> GetAllVTOForOrganization(UserOrganizationModel caller, long organizationId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);

          return s.QueryOver<VtoModel>().Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();
        }
      }
    }

    public static AngularVTO GetVTO(UserOrganizationModel caller, long vtoId, bool vision, bool traction, bool showIssues) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var vto = s.Get<VtoModel>(vtoId);
          var terms = TermsAccessor.GetTermsCollection(s, perms, vto.Organization.Id);
          return GetVTO(s, perms, vtoId, vision, traction, showIssues, terms);
        }
      }
    }

    public static AngularVTO GetSharedVTO(UserOrganizationModel caller, long orgId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetSharedVTO(s, perms, orgId);
        }
      }
    }

    public static AngularVTO GetSharedVTO(ISession s, PermissionsUtility perms, long orgId) {
      var l10 = s.QueryOver<L10Recurrence>()
        .Where(x => x.DeleteTime == null && x.OrganizationId == orgId && x.ShareVto == true)
        .Take(1).SingleOrDefault();

      if (l10 != null) {
        perms.Or(x => x.ViewOrganization(orgId), x => x.ViewL10Recurrence(l10.Id));
        var terms = TermsAccessor.GetTermsCollection(s, perms, orgId);
        var shared = GetVTO(s, perms, l10.VtoId, true, false, false, terms);
        return shared;
      }
      return null;
    }

    public static AngularVTO GetVTO(ISession s, PermissionsUtility perms, long vtoId, bool vision, bool traction, bool showIssues, TermsCollection terms) {
      if (!vision && !traction) {
        throw new PermissionsException();
      }
      var permsChecked = false;
      if (vision) {
        perms.ViewVTOVision(vtoId);
        permsChecked = true;
      }
      if (traction) {
        perms.ViewVTOTraction(vtoId);
        permsChecked = true;
      }

      if (showIssues) {
        //Overrides the value.
        showIssues = perms.IsPermitted(x => x.ViewVTOTractionIssues(vtoId));
      }

      if (!permsChecked)
        throw new PermissionsException();


      var model = s.Get<VtoModel>(vtoId);

      if (vision) {
        model._Values = OrganizationAccessor.GetCompanyValues_Unsafe(s.ToQueryProvider(true), model.Organization.Id, null);
      }
      var uniquesQ = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_Uniques && x.Vto.Id == vtoId && x.DeleteTime == null).Future();
      var looksLikeQ = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_LookLike && x.Vto.Id == vtoId && x.DeleteTime == null).Future();
      var goalsQ = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_YearGoals && x.Vto.Id == vtoId && x.DeleteTime == null).Future();

      var threeYearHeadersQ = s.QueryOver<VtoItem_KV>().Where(x => x.Type == VtoItemType.Header_ThreeYearPicture && x.Vto.Id == vtoId && x.DeleteTime == null).Future();
      var oneYearHeadersQ = s.QueryOver<VtoItem_KV>().Where(x => x.Type == VtoItemType.Header_OneYearPlan && x.Vto.Id == vtoId && x.DeleteTime == null).Future();
      var rockHeadersQ = s.QueryOver<VtoItem_KV>().Where(x => x.Type == VtoItemType.Header_QuarterlyRocks && x.Vto.Id == vtoId && x.DeleteTime == null).Future();
      var rocksQ = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
        .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == model.L10Recurrence && x.VtoRock)
        .Future();

      if (traction) {
        model._Issues = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_Issues && x.Vto.Id == vtoId && x.DeleteTime == null).List().Select(x => new VtoIssue() {
          Id = x.Id,
          BaseId = x.BaseId,
          CopiedFrom = x.CopiedFrom,
          CreateTime = x.CreateTime,
          Data = x.Data,
          DeleteTime = x.DeleteTime,
          ForModel = x.ForModel,
          Ordering = x.Ordering,
          Type = x.Type,
          Vto = x.Vto,
        }).ToList();
      }
      if (vision) {
        var getMarketStrategy = s.QueryOver<MarketingStrategyModel>().Where(x => x.Vto == vtoId && x.DeleteTime == null).List();
        foreach (var item in getMarketStrategy) {
          item._Uniques = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_Uniques && x.MarketingStrategyId == item.Id && x.DeleteTime == null).List().ToList();
        }

        model._MarketingStrategyModel = getMarketStrategy.ToList();

        model.ThreeYearPicture._Headers = threeYearHeadersQ.ToList();
        model.ThreeYearPicture._LooksLike = looksLikeQ.ToList();
      }
      if (traction) {

        model.OneYearPlan._Headers = oneYearHeadersQ.ToList();
        model.OneYearPlan._GoalsForYear = goalsQ.ToList();

        model.QuarterlyRocks._Headers = rockHeadersQ.ToList();
        model.QuarterlyRocks._Rocks = rocksQ.ToList().Where(x => x.ForRock.DeleteTime == null)
          .Select(x => AngularVtoRock.Create(x)).ToList();

        var issuesAttachedToRecur = model._Issues
          .Where(x => x.ForModel != null && x.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>())
          .Select(x => x.ForModel.ModelId)
          .Distinct().ToArray();

        if (issuesAttachedToRecur.Any()) {
          var foundIssues = s.QueryOver<IssueModel.IssueModel_Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(issuesAttachedToRecur).List().ToList();
          foreach (var i in model._Issues) {
            if (i.ForModel != null && i.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>()) {
              i.Issue = foundIssues.FirstOrDefault(x => x.Id == i.ForModel.ModelId);
              i._Extras["Owner"] = i.Issue.NotNull(x => x.Owner.NotNull(y => y.GetName()));
              i._Extras["OwnerInitials"] = i.Issue.NotNull(x => x.Owner.NotNull(y => y.GetInitials()));
            }
          }
        }
      }

      var output = AngularVTO.Create(model, terms);
      if (!vision) {
        output.CoreValueTitle = null;
        output.CoreFocus = null;
        output.IncludeVision = false;
        output.Strategies = null;
        output.Strategy = null;
        output.TenYearTarget = null;
        output.TenYearTargetTitle = null;
        output.ThreeYearPicture = null;
        output.Values = null;
      }

      if (!traction) {
        output.Issues = null;
        output.IssuesListTitle = null;
        output.OneYearPlan = null;
        output.QuarterlyRocks = null;
        output._TractionPageName = null;
        output.IncludeTraction = false;
      }


      if (!showIssues) {
        output.Issues = null;
        output.IssuesDisabled = true;
      }

      return output;
    }

    public static AngularVTO GetAngularVTO(UserOrganizationModel caller, long vtoId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);

          var vision = perms.IsPermitted(x => x.ViewVTOVision(vtoId));
          var traction = perms.IsPermitted(x => x.ViewVTOTraction(vtoId));
          var showIssues = perms.IsPermitted(x => x.ViewVTOTractionIssues(vtoId));
          if (!vision && !traction) {
            throw new PermissionsException("Cannot view this item");
          }

          var vto = s.Get<VtoModel>(vtoId);
          var terms = TermsAccessor.GetTermsCollection(s, perms, vto.Organization.Id);

          var ang = GetVTO(s, perms, vtoId, vision, traction, showIssues, terms);

          if (ang.L10Recurrence != null) {
            try {
              var recur = L10Accessor.GetL10Recurrence(s, perms, ang.L10Recurrence.Value, LoadMeeting.False());
              var orgVto = GetSharedVTO(s, perms, ang._OrganizationId);
              if (recur.TeamType != L10TeamType.LeadershipTeam && orgVto == null) {
                ang.IncludeVision = false;
              }
              if (orgVto != null) {
                ang.ReplaceVision(orgVto, terms);

              }

            } catch (Exception) {

            }
          }
          return ang;
        }
      }
    }
    public static VtoModel CreateRecurrenceVTO(ISession s, PermissionsUtility perm, long recurrenceId) {
      perm.EditL10Recurrence(recurrenceId);
      var recurrence = s.Get<L10Recurrence>(recurrenceId);
      perm.ViewOrganization(recurrence.OrganizationId);

      var model = new VtoModel();
      model.Organization = s.Get<OrganizationModel>(recurrence.OrganizationId);

      s.SaveOrUpdate(model);

      model.CoreFocus.Vto = model.Id;
      model.MarketingStrategy.Vto = model.Id;



      model.OneYearPlan.Vto = model.Id;
      model.OneYearPlan.FutureDate = DateTime.Now.Date;
      model.QuarterlyRocks.Vto = model.Id;
      model.QuarterlyRocks.FutureDate = DateTime.Now.Date;
      model.ThreeYearPicture.Vto = model.Id;
      model.ThreeYearPicture.FutureDate = DateTime.Now.Date;
      model.L10Recurrence = recurrenceId;

      model.Name = recurrence.Name;

      s.Update(model);

      recurrence.VtoId = model.Id;
      s.Update(recurrence);
      return model;
    }

    public static VtoModel CreateVTO(ISession s, PermissionsUtility perm, long organizationId) {
      perm.ViewOrganization(organizationId).CreateVTO(organizationId);

      var model = new VtoModel();
      model.Organization = s.Get<OrganizationModel>(organizationId);
      s.SaveOrUpdate(model);

      model.CoreFocus.Vto = model.Id;
      model.MarketingStrategy.Vto = model.Id;
      model.OneYearPlan.Vto = model.Id;
      model.QuarterlyRocks.Vto = model.Id;
      model.ThreeYearPicture.Vto = model.Id;

      s.Update(model);
      return model;
    }
    public static VtoModel CreateVTO(UserOrganizationModel caller, long organizationId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perm = PermissionsUtility.Create(s, caller);

          var model = CreateVTO(s, perm, organizationId);

          tx.Commit();
          s.Flush();

          return model;
        }
      }
    }


    public static MarketingStrategyModel CreateMarketingStrategy(UserOrganizationModel caller, TermsCollection terms, long vtoId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perm = PermissionsUtility.Create(s, caller).EditVTO(vtoId);
          MarketingStrategyModel obj = new MarketingStrategyModel();
          obj.Vto = vtoId;
          MarkUpdated(vtoId);

          var getMarketStrategies = s.QueryOver<MarketingStrategyModel>().Where(x => x.Vto == vtoId && x.DeleteTime == null).List();
          obj.Title = terms.GetTerm(TermKey.MarketingStrategy)+ (getMarketStrategies.Count == 0 ? "" : " " + (getMarketStrategies.Count + 1).ToString());

          s.Save(obj);
          tx.Commit();
          s.Flush();
          return obj;
        }
      }
    }


    public static async Task RemoveMarketingStrategy(UserOrganizationModel caller, long strategyId, string connectionId) {

      long vtoId = -1;
      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateStrategy(strategyId), async (s, perms) => {
        var strategy = s.Get<MarketingStrategyModel>(strategyId);
        perms.EditVTO(strategy.Vto);
      }, async s => {
        var strategy = s.Get<MarketingStrategyModel>(strategyId);
        strategy.DeleteTime = DateTime.UtcNow;
        s.Update(strategy);
        vtoId = strategy.Vto;
      }, async (s,perms) => {
        //unsynced updates;
        if (vtoId>0) {
          MarkUpdated(vtoId);
          await using (var rt = RealTimeUtility.Create()) {
            var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateVtoGroupId(vtoId));
            group.Update(new AngularVTO(vtoId) {
              Strategies = AngularList.CreateFrom(AngularListType.Remove, new AngularStrategy(strategyId))
            });
          }
        }
      });


    }


    public static async Task UpdateVtoString(UserOrganizationModel caller, long vtoStringId, String message, bool? deleted, string connectionId = null) {
      long? update_VtoId = null;
      VtoItem_String str = null;
      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateVtoItem(vtoStringId), async (s, perms) => {
        var str1 = s.Get<VtoItem_String>(vtoStringId);
        perms.EditVTO(str1.Vto.Id);
      }, async s => {
        str = s.Get<VtoItem_String>(vtoStringId);
        //var perm = PermissionsUtility.Create(s, caller).EditVTO(str.Vto.Id);
        str.Data = message;
        if (str.BaseId == 0) {
          str.BaseId = str.Id;
        }
        if (deleted != null) {
          if (deleted == true && str.DeleteTime == null) {
            str.DeleteTime = DateTime.UtcNow;
            connectionId = null;
          } else if (deleted == false) {
            str.DeleteTime = null;
          }
        }
        s.Update(str);
        update_VtoId = str.Vto.Id;
      }, async (s,perms) => {

        //unsynced updates.
        if (update_VtoId != null) {
          //var perms = PermissionsUtility.Create(s, caller);

          MarkUpdated(str.Vto.Id);
          //Update IssueRecurrence
          if (str.ForModel != null) {
            if (str.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>()) {
              var issueRecur = s.Get<IssueModel.IssueModel_Recurrence>(str.ForModel.ModelId);
              if (perms.IsPermitted(x => x.EditL10Recurrence(issueRecur.Recurrence.Id))) {
                issueRecur.Issue.Message = message;
                s.Update(issueRecur.Issue);
              }
            }
          }
        }
      });

      //unsynced updates.
      if (update_VtoId != null) {
        await using (var rt = RealTimeUtility.Create(connectionId)) {
          var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateVtoGroupId(update_VtoId.Value));
          //str.Vto = null;
          group.Update(AngularVtoString.Create(str));
        }
      }

    }

    public static async Task UpdateVtoKV(UserOrganizationModel caller, long vtoKVId, string key, string value, bool? deleted, string connectionId = null) {
      long? update_VtoId = null;
      VtoItem_KV kv = null;

      //Perform the update.
      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateVtoItem(vtoKVId), async (s, perms) => {
        var kv1 = s.Get<VtoItem_KV>(vtoKVId);
        perms.EditVTO(kv1.Vto.Id);
      }, async s => {
        kv = s.Get<VtoItem_KV>(vtoKVId);
        //var perm = PermissionsUtility.Create(s, caller).EditVTO(kv.VtoId);
        kv.K = key;
        kv.V = value;
        if (kv.BaseId == 0) {
          kv.BaseId = kv.Id;
        }

        if (deleted != null) {
          if (deleted == true && kv.DeleteTime == null) {
            kv.DeleteTime = DateTime.UtcNow;
            connectionId = null;
          } else if (deleted == false) {
            kv.DeleteTime = null;
          }
        }
        s.Update(kv);
        update_VtoId = kv.Vto.Id;
      }, null);
      if (update_VtoId != null) {
        MarkUpdated(update_VtoId.Value);
        await using (var rt = RealTimeUtility.Create(connectionId)) {
          var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateVtoGroupId(update_VtoId.Value));
          kv.Vto = null;
          group.Update(AngularVtoKV.Create(kv));
        }
      }
    }


    [Untested("ESA")]
    public static async Task UpdateVto(UserOrganizationModel caller, long vtoId, String name = null, String tenYearTarget = null, String tenYearTargetTitle = null, String coreValueTitle = null, String issuesListTitle = null, string connectionId = null) {
      VtoModel vto = null;
      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateVto(vtoId), async (s, perms) => {
        perms.EditVTO(vtoId);
      }, async s => {
        vto = s.Get<VtoModel>(vtoId);
        vto.Name = name;
        vto.TenYearTarget = tenYearTarget;
        vto.TenYearTargetTitle = tenYearTargetTitle;
        vto.CoreValueTitle = coreValueTitle;
        vto.IssuesListTitle = issuesListTitle;

        s.Update(vto);
      }, null);

      await using (var rt = RealTimeUtility.Create(connectionId)) {
        var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateVtoGroupId(vtoId));
        MarkUpdated(vtoId);

        if (vto!=null) {
          group.Update(new AngularVTO(vtoId) {
            Name = vto.Name,
            TenYearTarget = vto.TenYearTarget,
            TenYearTargetTitle = vto.TenYearTargetTitle,
            CoreValueTitle = vto.CoreValueTitle,
            IssuesListTitle = vto.IssuesListTitle
          });
        }
      }

    }

    public static async Task Update(UserOrganizationModel caller, BaseAngular model, string connectionId) {
      if (model.Type == typeof(AngularVtoString).Name) {
        var m = (AngularVtoString)model;
        await UpdateVtoString(caller, m.Id, m.Data, null, connectionId);
      } else if (model.Type == typeof(AngularVTO).Name) {
        var m = (AngularVTO)model;
        await UpdateVto(caller, m.Id, m.Name, m.TenYearTarget, m.TenYearTargetTitle, m.CoreValueTitle, m.IssuesListTitle, connectionId);
      } else if (model.Type == typeof(AngularCompanyValue).Name) {
        var m = (AngularCompanyValue)model;
        await UpdateCompanyValue(caller, m.Id, m.CompanyValue, m.CompanyValueDetails, null, connectionId);
      } else if (model.Type == typeof(AngularCoreFocus).Name) {
        var m = (AngularCoreFocus)model;
        var terms = TermsAccessor.GetTermsCollection(caller, caller.Organization.Id);
        await UpdateCoreFocus(caller, m.Id, m.Purpose, m.Niche, m.PurposeTitle, m.NicheTitle, m.CoreFocusTitle, connectionId, terms);
      } else if (model.Type == typeof(AngularStrategy).Name) {
        var m = (AngularStrategy)model;
        await UpdateStrategy(caller, m.Id, m.TargetMarket, m.ProvenProcess, m.Guarantee, m.MarketingStrategyTitle, m.Title, connectionId);
      } else if (model.Type == typeof(AngularVtoRock).Name) {
        var m = (AngularVtoRock)model;
        await UpdateRock(caller, m.Id, m.Rock.Name, m.Rock.Owner.Id, null, connectionId);
      } else if (model.Type == typeof(AngularOneYearPlan).Name) {
        var m = (AngularOneYearPlan)model;
        await UpdateOneYearPlan(caller, m.Id, m.FutureDate, m.OneYearPlanTitle, connectionId);
      } else if (model.Type == typeof(AngularQuarterlyRocks).Name) {
        var m = (AngularQuarterlyRocks)model;
        await UpdateQuarterlyRocks(caller, m.Id, m.FutureDate, m.RocksTitle, connectionId: connectionId);
      } else if (model.Type == typeof(AngularThreeYearPicture).Name) {
        var m = (AngularThreeYearPicture)model;
        await UpdateThreeYearPicture(caller, m.Id, m.FutureDate, m.ThreeYearPictureTitle, connectionId);
      } else if (model.Type == typeof(AngularVtoKV).Name) {
        var m = (AngularVtoKV)model;
        await UpdateVtoKV(caller, m.Id, m.K, m.V, null, connectionId);
      }
    }

    public static async Task UpdateThreeYearPicture(UserOrganizationModel caller, long id, DateTime? futuredate = null, string threeYearPictureTitle = null, string connectionId = null) {
      long vtoId = -1;

      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateThreeYearPicture(id), async (s, perms) => {
        var threeYear = s.Get<ThreeYearPictureModel>(id);
        vtoId = threeYear.Vto;
        perms.EditVTO(vtoId);
      }, async s => {
        var threeYear = s.Get<ThreeYearPictureModel>(id);
        vtoId = threeYear.Vto;
        threeYear.FutureDate = futuredate;
        threeYear.ThreeYearPictureTitle = threeYearPictureTitle;
        s.Update(threeYear);
      }, null);

      if (vtoId>0) {
        MarkUpdated(vtoId);
        await using (var rt = RealTimeUtility.Create(connectionId)) {
          var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateVtoGroupId(vtoId));
          group.Update(new AngularThreeYearPicture(id) {
            FutureDate = futuredate,
            ThreeYearPictureTitle = threeYearPictureTitle
          });
        }
      }
    }
    public static async Task UpdateQuarterlyRocks(UserOrganizationModel caller, long quarterlyRockModelId, DateTime? futuredate = null, string revenue = null, string profit = null, string measurables = null, string rocksTitle = null, string connectionId = null) {
      long vtoId = -1;

      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateQuarterlyRocks(quarterlyRockModelId), async (s, perms) => {
        var quarterlyRocks = s.Get<QuarterlyRocksModel>(quarterlyRockModelId);
        var vtoId = quarterlyRocks.Vto;
        perms.EditVTO(vtoId);
      }, async s => {
        var quarterlyRocks = s.Get<QuarterlyRocksModel>(quarterlyRockModelId);
        var vtoId = quarterlyRocks.Vto;
        quarterlyRocks.FutureDate = futuredate.HasValue ? futuredate.Value.Date : futuredate;
        quarterlyRocks.RocksTitle = rocksTitle;
        s.Update(quarterlyRocks);
      }, null);

      if (vtoId>0) {
        MarkUpdated(vtoId);
        await using (var rt = RealTimeUtility.Create(connectionId)) {
          var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateVtoGroupId(vtoId));
          group.Update(new AngularQuarterlyRocks(quarterlyRockModelId) {
            FutureDate = futuredate,
            RocksTitle = rocksTitle,
          });
        }
      }

    }

    public static async Task UpdateOneYearPlan(UserOrganizationModel caller, long id, DateTime? futuredate = null, string oneYearPlanTitle = null, string connectionId = null) {
      long vtoId = -1;

      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateOneYearPlan(id), async (s, perms) => {
        var plan = s.Get<OneYearPlanModel>(id);
        vtoId = plan.Vto;
        perms.EditVTO(vtoId);
      }, async s => {
        var plan = s.Get<OneYearPlanModel>(id);
        vtoId = plan.Vto;
        plan.FutureDate = futuredate.HasValue ? futuredate.Value.Date : futuredate;
        plan.OneYearPlanTitle = oneYearPlanTitle;
        s.Update(plan);
      },null);

      if (vtoId>0) {
        MarkUpdated(vtoId);
        await using (var rt = RealTimeUtility.Create(connectionId)) {
          var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateVtoGroupId(vtoId));
          group.Update(new AngularOneYearPlan(id) {
            FutureDate = futuredate,
            OneYearPlanTitle = oneYearPlanTitle
          });
        }
      }
    }

    public static async Task UpdateStrategy(UserOrganizationModel caller, long strategyId, String targetMarket = null, String provenProcess = null, String guarantee = null, String marketingStrategyTitle = null, String title = null, string connectionId = null) {

      long vtoId = -1;

      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateStrategy(strategyId), async (s, perms) => {
        var strategy = s.Get<MarketingStrategyModel>(strategyId);
        vtoId = strategy.Vto;
        perms.EditVTO(vtoId);
      }, async s => {
        var strategy = s.Get<MarketingStrategyModel>(strategyId);
        vtoId = strategy.Vto;
        strategy.ProvenProcess = provenProcess;
        strategy.Guarantee = guarantee;
        strategy.TargetMarket = targetMarket;
        strategy.MarketingStrategyTitle = marketingStrategyTitle;
        strategy.Title = title;
        s.Update(strategy);
      },null);

      //unsynced updates.
      if (vtoId>0) {
        MarkUpdated(vtoId);
        await using (var rt = RealTimeUtility.Create(connectionId)) {
          var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateVtoGroupId(vtoId));
          group.Update(new AngularStrategy(strategyId) {
            ProvenProcess = provenProcess,
            Guarantee = guarantee,
            TargetMarket = targetMarket,
            MarketingStrategyTitle = marketingStrategyTitle,
          });
        }
      }
    }

    public static async Task UpdateCoreFocus(UserOrganizationModel caller, long coreFocusId, string purpose, string niche, string purposeTitle, string nicheTitle, string coreFocusTitle, string connectionId, TermsCollection terms) {
      long vtoId = -1;
      CoreFocusModel coreFocus = null;

      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateCoreFocus(coreFocusId), async (s, perms) => {
        var coreFocus1 = s.Get<CoreFocusModel>(coreFocusId);
        perms.EditVTO(coreFocus1.Vto);
      }, async s => {
        coreFocus = s.Get<CoreFocusModel>(coreFocusId);
        coreFocus.Purpose = purpose;
        coreFocus.Niche = niche;
        coreFocus.PurposeTitle = purposeTitle;
        coreFocus.NicheTitle = nicheTitle;
        coreFocus.CoreFocusTitle = coreFocusTitle;
        s.Update(coreFocus);
        vtoId = coreFocus.Vto;
      },null);

      if (vtoId>0) {
        MarkUpdated(vtoId);
        if (coreFocus!=null) {
          var update = AngularCoreFocus.Create(coreFocus, terms);
          await UpdateVTO(vtoId, connectionId, x => x.Update(update));
        }
      }
    }

    public static async Task UpdateCompanyValue(UserOrganizationModel caller, long companyValueId, string message, string details, bool? deleted, string connectionId) {
      long orgId = -1;
      CompanyValueModel companyValue = null;
      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateCompanyValue(companyValueId), async (s, perms) => {
        var companyValue1 = s.Get<CompanyValueModel>(companyValueId);
        var orgId = companyValue1.OrganizationId;
        perms.EditCompanyValues(companyValue1.OrganizationId);
      }, async s => {
        companyValue = s.Get<CompanyValueModel>(companyValueId);
        orgId = companyValue.OrganizationId;
        //PermissionsUtility.Create(s, caller).EditCompanyValues(companyValue.OrganizationId);

        if (message != null) {
          companyValue.CompanyValue = message;
          s.Update(companyValue);
        }

        if (details != null) {
          companyValue.CompanyValueDetails = details;
          s.Update(companyValue);
        }

        if (deleted != null) {
          if (deleted == false) {
            companyValue.DeleteTime = null;
          } else if (companyValue.DeleteTime == null) {
            companyValue.DeleteTime = DateTime.UtcNow;
            connectionId = null;
          }
          s.Update(companyValue);
        }
      }, async (s, perms) => {
        if (orgId>0 && companyValue!=null) {
          var update = AngularCompanyValue.Create(companyValue);
          await UpdateAllVTOs(s, orgId, connectionId, x => x.Update(update));
        }
      });
    }


    public static async Task UpdateRock(UserOrganizationModel caller, long recurrenceRockId, string message, long? accountableUser, bool? deleted, string connectionId) {

      bool vtoRock = false;
      if (deleted==true) {
        vtoRock=false;
      } else if (deleted == false) {
        vtoRock=true;
      } else {
        //doesnt matter for vtoRock
      }
      var setVtoRockExecutor = new SetVtoRockExecutor(recurrenceRockId, vtoRock);
      var updateRockExecutor = new UpdateRockExecutor(-1, message, accountableUser);

      await SyncUtil.EnsureStrictlyAfter(caller, s => {
        //Sync Action
        var recurRock = s.Get<L10Recurrence.L10Recurrence_Rocks>(recurrenceRockId);
        return SyncAction.UpdateRockCompletion(recurRock.ForRock.Id);
      }, async (s, perms) => {
        if (deleted==true) {
          await setVtoRockExecutor.EnsurePermitted(s, perms);
        } else {
          if (deleted==false) {
            await setVtoRockExecutor.EnsurePermitted(s, perms);
          }
          var recurRock = s.Get<L10Recurrence.L10Recurrence_Rocks>(recurrenceRockId);
          updateRockExecutor.SetRockId(recurRock.ForRock.Id);
          await updateRockExecutor.EnsurePermitted(s, perms);
        }
      }, async s => {
        if (deleted == true) {
          await setVtoRockExecutor.AtomicUpdate(s);//await L10Accessor.SetVtoRock(s, perms, recurrenceRockId, false);
        } else {
          if (deleted == false) {
            await setVtoRockExecutor.AtomicUpdate(s);
          }
          await updateRockExecutor.AtomicUpdate(s);//await RockAccessor.UpdateRock(s, perms, recurRock.ForRock.Id, message, accountableUser);
        }
      }, async (s,perms)=> {

        if (deleted == true) {
          await setVtoRockExecutor.AfterAtomicUpdate(s,perms);//await L10Accessor.SetVtoRock(s, perms, recurrenceRockId, false);
        } else {
          if (deleted == false) {
            await setVtoRockExecutor.AfterAtomicUpdate(s, perms);
          }
          await updateRockExecutor.AfterAtomicUpdate(s, perms);//await RockAccessor.UpdateRock(s, perms, recurRock.ForRock.Id, message, accountableUser);
        }

      });
    }

    public static async Task JoinVto(IOuterSession s, PermissionsUtility perms, long callerId, long vtoId, string connectionId) {
      perms.Self(callerId);
      var caller = s.Get<UserOrganizationModel>(callerId);
      perms.ViewVTOVision(vtoId);

      await using (var rt = RealTimeUtility.Create(connectionId)) {
        await rt.AddToGroup(connectionId, RealTimeHub.Keys.GenerateVtoGroupId(vtoId));
      }
      Audit.VtoLog(s, caller, vtoId, "JoinVto");

    }

    public static async Task AddKV(UserOrganizationModel caller, long vtoId, VtoItemType type, Func<VtoModel, BaseAngularList<AngularVtoKV>, IAngularId> updateFunc, bool skipUpdate = false, ForModel forModel = null, string key = null, string value = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          await AddKV(s, perms, vtoId, type, updateFunc, skipUpdate, forModel, key, value);
          tx.Commit();
          s.Flush();
        }
      }
    }

    public static async Task<VtoItem_KV> AddKV(ISession s, PermissionsUtility perms, long vtoId, VtoItemType type, Func<VtoModel, BaseAngularList<AngularVtoKV>, IAngularId> updateFunc, bool skipUpdate = false, ForModel forModel = null, string key = null, string value = null) {
      perms.EditVTO(vtoId);
      var vto = s.Get<VtoModel>(vtoId);
      var organizationId = vto.Organization.Id;

      var items = s.QueryOver<VtoItem_KV>()
        .Where(x => x.Vto.Id == vtoId && x.Type == type && x.DeleteTime == null)
        .List().ToList();

      var count = items.Count();

      var kv = new VtoItem_KV() {
        Type = type,
        Ordering = count,
        Vto = vto,
        ForModel = forModel,
        K = key,
        V = value,
      };

      s.Save(kv);
      MarkUpdated(vto);

      items.Add(kv);
      var angularItems = AngularList.Create(AngularListType.ReplaceAll, AngularVtoKV.Create(items));

      if (updateFunc != null) {
        if (skipUpdate) {
          await UpdateVTO(vtoId, null, x => x.Update(updateFunc(vto, angularItems)));
        }
        await UpdateVTO(vtoId, null, x => x.Update(updateFunc(vto, angularItems)));
      }
      return kv;
    }

    public static async Task<VtoItem_String> AddString(ISession s, PermissionsUtility perms, long vtoId, VtoItemType type, Func<VtoModel, BaseAngularList<AngularVtoString>, IAngularId> updateFunc, ForModel forModel = null, string value = null, long? marketingStrategyId = null) {
      perms.EditVTO(vtoId);
      var vto = s.Get<VtoModel>(vtoId);
      var organizationId = vto.Organization.Id;

      var count = s.QueryOver<VtoItem_String>().Where(x => x.Vto.Id == vtoId && x.Type == type && x.DeleteTime == null && x.MarketingStrategyId == marketingStrategyId).RowCount();
      //var count = items.Count();

      var str = new VtoItem_String() {
        Type = type,
        Ordering = count,
        Vto = vto,
        ForModel = forModel,
        Data = value,
        MarketingStrategyId = marketingStrategyId
      };

      s.Save(str);
      MarkUpdated(vto);

      //items.Add(str);
      var angularItems = AngularList.CreateFrom(AngularListType.Add, AngularVtoString.Create(str));

      if (updateFunc != null) {
        //if (skipUpdate) {
          //await UpdateVTO(vtoId, null, x => x.Update(updateFunc(vto, angularItems)));
        //} else {
          await UpdateVTO(vtoId, null, x => x.Update(updateFunc(vto, angularItems)));
        //}
      }
      return str;
    }

    public static async Task AddString(UserOrganizationModel caller, long vtoId, VtoItemType type, Func<VtoModel, BaseAngularList<AngularVtoString>, IAngularId> updateFunc, ForModel forModel = null, long? marketingStrategyId = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          await AddString(s, perms, vtoId, type, updateFunc, forModel, null, marketingStrategyId);
          tx.Commit();
          s.Flush();
        }
      }
    }

    public static async Task AddUniques(UserOrganizationModel caller, long vtoId, long marketingStrategyId) {
      await AddString(caller, vtoId, VtoItemType.List_Uniques, (vto, items) => new AngularStrategy(marketingStrategyId) { Uniques = items }, null, marketingStrategyId);
    }
    public static async Task AddThreeYear(UserOrganizationModel caller, long vtoId) {
      await AddString(caller, vtoId, VtoItemType.List_LookLike, (vto, list) => new AngularThreeYearPicture(vto.ThreeYearPicture.Id) { LooksLike = list });
    }
    public static async Task AddYearGoal(UserOrganizationModel caller, long vtoId) {
      await AddString(caller, vtoId, VtoItemType.List_YearGoals, (vto, list) => new AngularOneYearPlan(vto.OneYearPlan.Id) { GoalsForYear = list });
    }
    public static async Task AddIssue(UserOrganizationModel caller, long vtoId) {
      await AddString(caller, vtoId, VtoItemType.List_Issues, (vto, list) => new AngularVTO(vto.Id) { Issues = list });
    }

    public static async Task AddCompanyValue(UserOrganizationModel caller, long vtoId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller).EditVTO(vtoId);

          var vto = s.Get<VtoModel>(vtoId);

          var organizationId = vto.Organization.Id;
          var existing = OrganizationAccessor.GetCompanyValues(s.ToQueryProvider(true), perms, organizationId, null);
          existing.Add(new CompanyValueModel() { OrganizationId = organizationId });
          await OrganizationAccessor.EditCompanyValues(s, perms, organizationId, existing);

          MarkUpdated(vto);
          tx.Commit();
          s.Flush();
        }
      }
    }

    public static async Task CreateNewRock(UserOrganizationModel caller, long vtoId, string message = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          await CreateNewRock(s, perms, vtoId, caller.Id, message);
          tx.Commit();
          s.Flush();
        }
      }
    }

    public static async Task CreateNewRock(ISession s, PermissionsUtility perms, long vtoId, long ownerId, string message = null) {
      var vto = s.Get<VtoModel>(vtoId);
      MarkUpdated(vto);
      await L10Accessor.CreateAndAttachRock(s, perms, vto.L10Recurrence.Value, ownerId, message, true);
    }

    private static string ParseVtoHeader(TableCell cell, string searchFor) {
      searchFor = searchFor.ToLower();
      var found = cell.Elements<Paragraph>().Where(x => x.ParagraphProperties.ParagraphStyleId.Val.Value != "ListParagraph")
        .Where(x => x.InnerText.ToLower().Contains(searchFor))
        .FirstOrDefault()
        .NotNull(x => x.InnerText);
      if (found != null) {
        var sp = found.Split(':');
        if (sp.Length > 1) {
          found = string.Join(":", sp.Skip(1));
        } else {
          found = sp[0].SubstringAfter(searchFor);
        }
      }
      return found;
    }

    public static async Task<VtoModel> UploadVtoForRecurrence(UserOrganizationModel caller, WordprocessingDocument doc, long recurrenceId, List<Exception> exceptions) {

      exceptions = exceptions ?? new List<Exception>();


      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller).AdminL10Recurrence(recurrenceId);
          var recur = s.Get<L10Recurrence>(recurrenceId);
          var vtoId = recur.VtoId;
          if (vtoId <= 0) {
            throw new PermissionsException("Business Plan does not exist.");
          }

          perms.EditVTO(vtoId);
          var vto = s.Get<VtoModel>(vtoId);
          if (vto == null) {
            throw new PermissionsException("Business Plan does not exist.");
          }

          #region Initialize defaults
          var corevaluesTitle = "CORE VALUES";
          var threeYearTitle = "3-YEAR VISION";
          var coreFocusTitle = "FOCUS";
          var tenYearTargetTitle = "BHAG";
          var marketingStrategyTitle = "MARKETING STRATEGY";
          var rocksTitle = "GOALS";
          var issuesTitle = "LONG-TERM ISSUES";
          var oneYearTitle = "1-YEAR GOALS";

          List<string> corevaluesList = new List<string>();

          string threeYearFuture = "";
          string threeYearRevenue = "";
          string threeYearProfit = "";
          string threeYearMeasurables = "";
          var threeYearLooksList = new List<string>();

          string purpose = "<could not parse>";
          string niche = "<could not parse>";
          string purposeTitle = "Purpose/Cause/Passion";

          TableCell tenYearCell = null;
          var marketingDict = new DefaultDictionary<string, string>(x => "<could not parse>");
          var uniques = new List<string>();

          string oneYearFuture = "";
          string oneYearRevenue = "";
          string oneYearProfit = "";
          string oneYearMeasurables = "";

          List<string> oneYearPlanGoals = new List<string>();
          string rocksFuture = "";
          string rocksRevenue = "";
          string rocksProfit = "";
          string rocksMeasurables = "";

          List<TableRow> rocksList = new List<TableRow>();
          List<string> issuesList = new List<string>();
          #endregion

          #region Page 1
          var body = doc.MainDocumentPart.Document.Body;
          try {
            if (body.Elements<Table>().Count() < 2) {
              throw new FormatException("Could not find the Business Plan.");
            }

            var page1 = body.Elements<Table>().ElementAt(0);
            var page1Rows = page1.Elements<TableRow>();
            if (page1Rows.Count() != 5) {
              throw new FormatException("Could not find Vision Page.");
            }

            var corevaluesRow = page1Rows.ElementAt(0).Elements<TableCell>();
            var threeYearPictureDetailsRow = page1Rows.ElementAt(1).Elements<TableCell>();
            var coreFocusRow = page1Rows.ElementAt(2).Elements<TableCell>();
            var tenYearRow = page1Rows.ElementAt(3).Elements<TableCell>();
            var marketingStrategyRow = page1Rows.ElementAt(4);

            try {
              //Core values
              if (corevaluesRow.Count() != 3) {
                throw new FormatException("Could not find Core Values.");
              }

              corevaluesTitle = GetHeaderTitle(corevaluesRow, 0) ?? corevaluesTitle;
            } catch (Exception e) {
              exceptions.Add(e);
            }
            try {
              //3 year picture
              if (corevaluesRow.Count() != 3 || coreFocusRow.Count() != 3) {
                throw new FormatException("Could not find Three Year Picture.");
              }

              if (corevaluesRow.ElementAt(2).Elements<Paragraph>().Count() == 1 && !string.IsNullOrWhiteSpace(corevaluesRow.ElementAt(2).Elements<Paragraph>().ElementAt(0).InnerText)) {
                threeYearTitle = corevaluesRow.ElementAt(2).Elements<Paragraph>().ElementAt(0).InnerText;
              }

              if (threeYearPictureDetailsRow.Count() != 3) {
                throw new FormatException("Could not find Three Year Picture details.");
              }

              var threeYearCell = threeYearPictureDetailsRow.ElementAt(2);


              //3 year picture - Headings
              try {
                threeYearFuture = ParseVtoHeader(threeYearCell, "Future Date");
                threeYearRevenue = ParseVtoHeader(threeYearCell, "Revenue");
                threeYearProfit = ParseVtoHeader(threeYearCell, "Profit");
                threeYearMeasurables = ParseVtoHeader(threeYearCell, "Measurables");
              } catch (Exception e) {
                exceptions.Add(new FormatException("Could not add Three Year Picture heading", e));
              }
              try {
                if (threeYearCell.Elements<Numbering>().Count() != 0) {
                  threeYearLooksList = threeYearCell.Elements<Numbering>().Last().Elements().Select(x => x.InnerText.Trim()).ToList();
                }
              } catch (Exception e) {
                exceptions.Add(new FormatException("Could not add Three Year Picture heading", e));
              }
            } catch (Exception e) {
              exceptions.Add(e);
            }
            try {
              //Core Focus
              if (coreFocusRow.Count() != 3) {
                throw new FormatException("Could not find Core Focus.");
              }

              if (coreFocusRow.ElementAt(0).Elements<Paragraph>().Count() == 1 && !string.IsNullOrWhiteSpace(coreFocusRow.ElementAt(0).Elements<Paragraph>().ElementAt(0).InnerText)) {
                coreFocusTitle = coreFocusRow.ElementAt(0).Elements<Paragraph>().ElementAt(0).InnerText;
              }

              var coreFocusCell = coreFocusRow.ElementAt(1);

              var nicheParagraphTuple = coreFocusCell.Elements<Paragraph>()
                                  .Select((x, i) => Tuple.Create(i, x))
                                  .Where(x => x.Item2.InnerText.ToLower().Contains("niche"))
                                  .FirstOrDefault();

              if (nicheParagraphTuple == null && coreFocusCell.Elements<Paragraph>().Count() == 2) {
                var firstSplit = coreFocusCell.Elements<Paragraph>().ElementAt(0).InnerText.Split(':');
                purposeTitle = firstSplit.Length > 1 ? firstSplit.First().Trim() : purposeTitle;
                purpose = firstSplit.Last().Trim();
                niche = coreFocusCell.Elements<Paragraph>().ElementAt(1).InnerText.Split(':').Last().Trim();
              } else {
                var purposeParagraphs = string.Join(" ", coreFocusCell.Elements<Paragraph>().Where((x, i) => i < nicheParagraphTuple.Item1).Select(x => x.InnerText).ToList());
                var nicheParagraphs = string.Join(" ", coreFocusCell.Elements<Paragraph>().Where((x, i) => i >= nicheParagraphTuple.Item1).Select(x => x.InnerText).ToList());

                purpose = purposeParagraphs.Split(':').Last().Trim();
                niche = nicheParagraphs.Split(':').Last().Trim();

              }
            } catch (Exception e) {
              exceptions.Add(new FormatException("Could not add Core Focus.", e));
            }

            try {
              //10 year target
              if (tenYearRow.Count() != 3) {
                throw new FormatException("Could not find Ten Year Target.");
              }

              if (tenYearRow.ElementAt(0).Elements<Paragraph>().Count() == 1 && !string.IsNullOrWhiteSpace(tenYearRow.ElementAt(0).Elements<Paragraph>().ElementAt(0).InnerText)) {
                tenYearTargetTitle = tenYearRow.ElementAt(0).Elements<Paragraph>().ElementAt(0).InnerText;
              }

              tenYearCell = tenYearRow.ElementAt(1);
            } catch (Exception e) {
              exceptions.Add(new FormatException("Could not add Ten Year Target.", e));
            }

            try {
              //Marketing Strategy
              if (marketingStrategyRow.Count() != 3) {
                throw new FormatException("Could not find Marketing Strategy.");
              }

              if ((marketingStrategyRow.ElementAt(0).Elements<Paragraph>().Count() == 1 || marketingStrategyRow.ElementAt(0).Elements<Paragraph>().Count() == 2) && !string.IsNullOrWhiteSpace(string.Join(" ", marketingStrategyRow.ElementAt(0).Elements<Paragraph>().Select(x => x.InnerText)).Trim())) {
                marketingStrategyTitle = string.Join(" ", marketingStrategyRow.ElementAt(0).Elements<Paragraph>().Select(x => x.InnerText)).Trim();
              }

              var marketingStrategyCell = marketingStrategyRow.ElementAt(1);


              var targetTuple = Tuple.Create("target", marketingStrategyCell.Elements<Paragraph>().Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.InnerText.ToLower().Contains("target market") || x.Item2.InnerText.Contains("The List")).FirstOrDefault());
              var uniquesTuple = Tuple.Create("uniques", marketingStrategyCell.Elements<Paragraph>().Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.InnerText.ToLower().Contains("uniques")).FirstOrDefault());
              var provenTuple = Tuple.Create("proven", marketingStrategyCell.Elements<Paragraph>().Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.InnerText.ToLower().Contains("proven")).FirstOrDefault());
              var guaranteeTuple = Tuple.Create("guarantee", marketingStrategyCell.Elements<Paragraph>().Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.InnerText.ToLower().Contains("guarantee")).FirstOrDefault());

              // <name, <location, paragraph>>
              var marketStratList = new List<Tuple<string, Tuple<int, Paragraph>>>() { targetTuple, uniquesTuple, provenTuple, guaranteeTuple };

              var ordering = marketStratList.Where(x => x.Item2 != null).OrderBy(x => x.Item2.Item1).ToList().Where(x => x.Item2.Item2 != null).ToList();


              if (ordering.Any()) {
                for (var i = 0; i < ordering.Count; i++) {
                  var start = ordering[i].Item2.Item1;
                  var end = 0;
                  if (i != ordering.Count - 1) {
                    end = ordering[i + 1].Item2.Item1;
                  } else {
                    end = marketingStrategyCell.Elements<Paragraph>().Count();
                  }
                  //Grab this section's paragraphs
                  //merge the magic text together, skip the first one (usually the title)
                  var sectionTitle = ordering[i].Item1;
                  marketingDict[sectionTitle] = string.Join("", marketingStrategyCell.Elements<Paragraph>().Where((x, j) => start <= j && j < end).Select(x => x.InnerText));
                }
              }
              if (marketingStrategyCell.Elements<Numbering>().Count() == 1) {
                uniques = marketingStrategyCell.Elements<Numbering>().ElementAt(0).Select(x => x.InnerText).ToList();
              } else if (marketingStrategyCell.Elements<Numbering>().Count() > 1) {
                var uniquesHeadingLoc = marketingStrategyCell.InnerXml.IndexOf(uniquesTuple.Item2.Item2.InnerXml);
                uniques = marketingStrategyCell.Elements<Numbering>()
                  .FirstOrDefault(x => marketingStrategyCell.InnerXml.IndexOf(x.InnerXml) > uniquesHeadingLoc)
                  .NotNull(y => y.Select(x => x.InnerText).ToList()) ?? uniques;
              }
            } catch (Exception e) {
              exceptions.Add(new FormatException("Could not add Marketing Strategy.", e));
            }
          } catch (Exception e) {
            exceptions.Add(e);
          }


          #endregion
          #region Page 2
          try {
            var page2 = body.Elements<Table>().ElementAt(1);


            var headingsRow = page2.Elements<TableRow>().ElementAt(0);
            var tractionRow = page2.Elements<TableRow>().ElementAt(1);


            if (headingsRow.Elements<TableCell>().Count() != 3) {
              throw new FormatException("Could not find Traction Page headings.");
            }
            if (tractionRow.Elements<TableCell>().Count() != 3) {
              throw new FormatException("Could not find Traction Page data.");
            }

            oneYearTitle = GetHeaderTitle(headingsRow, 0) ?? oneYearTitle;
            rocksTitle = GetHeaderTitle(headingsRow, 1) ?? rocksTitle;
            issuesTitle = GetHeaderTitle(headingsRow, 2) ?? issuesTitle;


            //One Year Plan
            try {
              var oneYearPlanCell = tractionRow.Elements<TableCell>().ElementAt(0);
              if (oneYearPlanCell.Elements<Table>().Count() != 1) {
                throw new FormatException("Could not find One Year Plan goals.");
              }
              //One year target - Headings
              try {
                oneYearFuture = ParseVtoHeader(oneYearPlanCell, "Future Date");
                oneYearRevenue = ParseVtoHeader(oneYearPlanCell, "Revenue");
                oneYearProfit = ParseVtoHeader(oneYearPlanCell, "Profit");
                oneYearMeasurables = ParseVtoHeader(oneYearPlanCell, "Measurables");
              } catch (Exception e) {
                exceptions.Add(new FormatException("Could not add One Year Goals heading.", e));
              }

              try {
                oneYearPlanGoals = oneYearPlanCell.Elements<Table>().ElementAt(0).Elements<TableRow>()
                  .Select(x => string.Join("\n", x.Elements<TableCell>().Last().Elements<Paragraph>().Select(y => y.InnerText)))
                  .Where(x => !string.IsNullOrWhiteSpace(x))
                  .ToList();
              } catch (Exception e) {
                exceptions.Add(new FormatException("Could not add One Year Goals.", e));
              }

            } catch (Exception e) {
              exceptions.Add(e);
            }

            //Goals (Used to be Rocks)
            try {
              var rocksCell = tractionRow.Elements<TableCell>().ElementAt(1);

              try {
                //One year target - Headings
                rocksFuture = ParseVtoHeader(rocksCell, "Future Date");
                rocksRevenue = ParseVtoHeader(rocksCell, "Revenue");
                rocksProfit = ParseVtoHeader(rocksCell, "Profit");
                rocksMeasurables = ParseVtoHeader(rocksCell, "Measurables");
              } catch (Exception e) {
                exceptions.Add(new FormatException("Could not add Goals heading.", e));
              }
              try {
                var bestTable = rocksCell.Elements<Table>().OrderByDescending(x => ColumnCount(x)).Where(x => ColumnCount(x) <= 3).FirstOrDefault();

                if (bestTable == null) {
                  throw new FormatException("Could not find Goals list.");
                }
                rocksList = bestTable.Elements<TableRow>().ToList();
              } catch (Exception e) {
                exceptions.Add(new FormatException("Could not add Goals.", e));
              }
            } catch (Exception e) {
              exceptions.Add(e);
            }
            //Issues List
            try {
              var issuesCell = tractionRow.Elements<TableCell>().ElementAt(2);
              if (issuesCell.Elements<Table>().Count() != 1 || ColumnCount(issuesCell.Elements<Table>().ElementAt(0)) > 2) {
                throw new FormatException("Could not find Issues List.");
              }

              issuesList = issuesCell.Elements<Table>().ElementAt(0).Elements<TableRow>()
                .Select(x => string.Join("\n", x.Elements<TableCell>().Last().Elements<Paragraph>().Select(y => y.InnerText.Trim())))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            } catch (Exception e) {
              exceptions.Add(new FormatException("Could not add Issues List.", e));
            }
            #endregion
          } catch (Exception e) {
            exceptions.Add(e);
          }
          #region Update VTO
          //Headings
          vto.CoreValueTitle = corevaluesTitle;
          vto.CoreFocus.CoreFocusTitle = coreFocusTitle;
          vto.TenYearTargetTitle = tenYearTargetTitle;
          vto.MarketingStrategy.MarketingStrategyTitle = marketingStrategyTitle;
          vto.ThreeYearPicture.ThreeYearPictureTitle = threeYearTitle;

          vto.OneYearPlan.OneYearPlanTitle = oneYearTitle;
          vto.QuarterlyRocks.RocksTitle = rocksTitle;
          vto.IssuesListTitle = issuesTitle;


          //Core Values
          var organizationId = vto.Organization.Id;
          var existing = OrganizationAccessor.GetCompanyValues(s.ToQueryProvider(true), perms, organizationId, null);
          foreach (var cv in corevaluesList) {
            existing.Add(new CompanyValueModel() { OrganizationId = organizationId, CompanyValue = cv });
          }
          await OrganizationAccessor.EditCompanyValues(s, perms, organizationId, existing);

          var currencyStyle = NumberStyles.AllowCurrencySymbol
            | NumberStyles.AllowDecimalPoint
            | NumberStyles.AllowLeadingSign
            | NumberStyles.AllowLeadingWhite
            | NumberStyles.AllowParentheses
            | NumberStyles.AllowThousands
            | NumberStyles.AllowTrailingWhite
            | NumberStyles.Currency;

          var currentCulture = Thread.CurrentThread.CurrentCulture;
          //Three Year Picture
          vto.ThreeYearPicture.FutureDate = threeYearFuture.TryParseDateTime();
          await VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_ThreeYearPicture, null, skipUpdate: true, key: "Revenue", value: threeYearRevenue);
          await VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_ThreeYearPicture, null, skipUpdate: true, key: "Profit", value: threeYearProfit);
          await VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_ThreeYearPicture, null, skipUpdate: true, key: "Measurables", value: threeYearMeasurables);

          foreach (var t in threeYearLooksList) {
            await VtoAccessor.AddString(s, perms, vtoId, VtoItemType.List_LookLike, null, value: t);
          }

          //Core Focus 
          vto.CoreFocus.Niche = niche;
          vto.CoreFocus.Purpose = purpose;
          vto.CoreFocus.PurposeTitle = purposeTitle;


          //Ten Year Target
          if (tenYearCell != null) {
            vto.TenYearTarget = string.Join("\n", tenYearCell.Elements<Paragraph>().Select(x => x.InnerText));
          }

          //Marketing Strategy 

          vto.MarketingStrategy.TargetMarket = marketingDict["target"];
          vto.MarketingStrategy.ProvenProcess = marketingDict["proven"];
          vto.MarketingStrategy.Guarantee = marketingDict["guarantee"];

          foreach (var t in uniques) {
            await VtoAccessor.AddString(s, perms, vtoId, VtoItemType.List_Uniques, null, value: t);
          }

          //One Year Plan
          vto.OneYearPlan.FutureDate = oneYearFuture.TryParseDateTime();
          await VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_OneYearPlan, null, skipUpdate: true, key: "Revenue", value: oneYearRevenue);
          await VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_OneYearPlan, null, skipUpdate: true, key: "Profit", value: oneYearProfit);
          await VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_OneYearPlan, null, skipUpdate: true, key: "Measurables", value: oneYearMeasurables);

          foreach (var t in oneYearPlanGoals) {
            await VtoAccessor.AddString(s, perms, vtoId, VtoItemType.List_YearGoals, null, value: t);
          }

          //Goals (Used to be Rocks)
          vto.QuarterlyRocks.FutureDate = rocksFuture.TryParseDateTime();
          await VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_QuarterlyRocks, null, skipUpdate: true, key: "Revenue", value: rocksRevenue);
          await VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_QuarterlyRocks, null, skipUpdate: true, key: "Profit", value: rocksProfit);
          await VtoAccessor.AddKV(s, perms, vtoId, VtoItemType.Header_QuarterlyRocks, null, skipUpdate: true, key: "Measurables", value: rocksMeasurables);

          var allUsers = TinyUserAccessor.GetOrganizationMembers(s, perms, vto.Organization.Id);
          Dictionary<string, DiscreteDistribution<TinyUser>> rockUserLookup = null;
          if (rocksList.Any() && (ColumnCount(rocksList[0]) == 3 || ColumnCount(rocksList[0]) == 2)) {
            var rockUsers = rocksList.Select(x => string.Join("\n", x.Elements<TableCell>().Last().Elements<Paragraph>().Select(y => y.InnerText)));
            rockUserLookup = DistanceUtility.TryMatch(rockUsers, allUsers);
          }

          try {
            foreach (var r in rocksList) {
              var owner = caller.Id;
              if (ColumnCount(r) == 2 || ColumnCount(r) == 3) {
                var ownerTup = new TinyUser() {
                  FirstName = "",
                  LastName = "",
                  UserOrgId = owner
                };
                rockUserLookup[string.Join("\n", r.Elements<TableCell>().Last().Elements<Paragraph>().Select(y => y.InnerText))].TryResolveOne(ref ownerTup);
              }

              var message = r.Elements<TableCell>().Reverse().Skip(1).FirstOrDefault().NotNull(x => string.Join("\n", x.Elements<Paragraph>().Select(y => y.InnerText)));
              if (!string.IsNullOrWhiteSpace(message)) {
                await CreateNewRock(s, perms, vtoId, owner, message);
              }
            }
          } catch (Exception e) {
            exceptions.Add(new FormatException("Could not upload Goals.", e));
          }

          //Issues
          foreach (var i in issuesList) {
            await VtoAccessor.AddString(s, perms, vtoId, VtoItemType.List_Issues, null, value: i);
          }
          #endregion

          tx.Commit();
          s.Flush();

          return vto;
        }
      }
    }

    private static int ColumnCount(TableRow tableRow) {
      return tableRow.Elements<TableCell>().Count();
    }

    private static int ColumnCount(Table table) {
      var rows = table.Elements<TableRow>();
      if (rows.Count() == 0)
        return 0;
      return rows.First().Elements<TableCell>().Count();
    }

    private static string GetHeaderTitle(TableRow headingsRow, int colNum) {
      return GetHeaderTitle(headingsRow.Elements<TableCell>(), colNum);
    }

    private static string GetHeaderTitle(IEnumerable<TableCell> rowCells, int colNum) {
      var cell = rowCells.ElementAt(colNum);
      if (cell.Elements<Paragraph>().Count() == 1 && !string.IsNullOrWhiteSpace(cell.Elements<Paragraph>().ElementAt(0).InnerText)) {
        return cell.Elements<Paragraph>().ElementAt(0).InnerText;
      }
      return null;
    }

    public static VtoItem_String GetVTOIssueByIssueId(UserOrganizationModel caller, long issueId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var vtoItem = GetVTOIssueByIssueId(s, perms, issueId);
          perms.ViewVTOTraction(vtoItem.Vto.Id);
          return vtoItem;
        }
      }
    }

    public static VtoItem_String GetVTOIssueByIssueId(ISession session, PermissionsUtility perms, long issueId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          // Currently, there is no database constraint ensuring the uniqueness of 'ForModel_ModelId' elements in the 'VtoItem' table,
          // which could lead to duplicates and cause exceptions. For this reason, the query is constructed as follows:
          var vtoItem = s.QueryOver<VtoItem_String>().Where(x => x.DeleteTime == null && x.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>() && x.ForModel.ModelId == issueId)
                            .Take(1)
                            .List()
                            .FirstOrDefault();

          if (vtoItem is null)
          {
            return null;
          }

          perms.ViewVTOTraction(vtoItem.Vto.Id);
          return vtoItem;
        }
      }
    }

    public static List<ModelIssue> GetAllVTOIssue(UserOrganizationModel caller, long recurrenceId)
    {
      using var session = HibernateSession.GetCurrentSession();
      var perms = PermissionsUtility.Create(session, caller);

      var recurrence = session.Get<L10Recurrence>(recurrenceId);
      var vtoId = recurrence.VtoId;
      perms.ViewVTOTraction(vtoId);

      var vtoItem = session.QueryOver<VtoItem_String>().Where(x => x.Vto.Id == vtoId &&
                                                              x.DeleteTime == null &&
                                                              x.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>()).List();

      var issueVtoId = vtoItem
          .Where(x => x.ForModel != null && x.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>())
          .Select(x => x.ForModel.ModelId)
          .Distinct().ToArray();

      var foundIssues = session.QueryOver<IssueModel.IssueModel_Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(issueVtoId).List().ToList();
      var issuesconverted = foundIssues.Select(i =>
      {
        var issue = IssueTransformer.IssueFromIssueRecurrence(i);
        issue.AddToDepartmentPlan = true;
        return issue;
      }).ToList();

      return issuesconverted;
    }

    public static List<ModelIssue> GetAllVTOIssueByRecurrenceIds(UserOrganizationModel caller, IReadOnlyList<long> recurrenceIds)
    {
      using var session = HibernateSession.GetCurrentSession();
      var perms = PermissionsUtility.Create(session, caller);

      var vtoIds = session.QueryOver<L10Recurrence>()
                    .WhereRestrictionOn(r => r.Id).IsIn(recurrenceIds.ToArray())
                    .Select(r => r.VtoId)
                    .List<long>()
                    .ToList();

      vtoIds = vtoIds.Where(vtoId => perms.IsVtoTractionViewable(vtoId)).ToList();

      var vtoItem = session.QueryOver<VtoItem_String>()
    .Where(x => x.DeleteTime == null &&
                x.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>() &&
                x.Vto.IsIn(vtoIds))
                .List();

      var issueVtoId = vtoItem
          .Where(x => x.ForModel != null && x.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>())
          .Select(x => x.ForModel.ModelId)
          .Distinct().ToArray();

      var foundIssues = session.QueryOver<IssueModel.IssueModel_Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(issueVtoId).List().ToList();
      var issuesconverted = foundIssues.Select(i =>
      {
        var issue = IssueTransformer.IssueFromIssueRecurrence(i);
        issue.AddToDepartmentPlan = true;
        return issue;
      }).ToList();

      return issuesconverted;
    }
  }
}
