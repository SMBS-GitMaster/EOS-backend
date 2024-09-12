using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using System.Threading.Tasks;
using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Core.Utilities.Permissions.Models;
using NHibernate.Criterion;

namespace RadialReview.Accessors
{
  public class MetricTabAccessor
  {

    #region Public Methods

    public static async Task<long> AddMetricTab(UserOrganizationModel caller, string title, UnitType units, Frequency frequency, bool isPinned, long? meetingId, bool shareToMeeting)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = MetricTabPermissions.Create(s, caller);
          perms.ViewUserOrganization(caller.Id, false);
          perms.CanCreateMetricTab((long) meetingId);

          MetricTabModel tab = new MetricTabModel()
          {
            CreatedTimestamp = DateTime.UtcNow,
            DateLastModified = DateTime.UtcNow,
            UserId = caller.Id,
            Title = title,
            Units = units,
            Frequency = frequency,
            ShareToMeeting = shareToMeeting,
            MeetingId = meetingId,
          };

          s.Save(tab);

          await HooksRegistry.Each<IMetricHook>((sess, x) => x.CreateMetricTab(sess, caller, tab));

          tx.Commit();
          s.Flush();

          await MetricsTabPinnedAccessor.SetPinnedForMetric(caller, tab.Id, isPinned);

          return tab.Id;

        }
      }
    }

    public static async Task<long> AddMetricToTab(UserOrganizationModel caller, long tabId, long metricId, TrackedMetricColor color)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = MetricTabPermissions.Create(s, caller, tabId);
          perms.ViewUserOrganization(caller.Id, false);
          perms.CanEditMetricTab();

          TrackedMetricModel metric = new TrackedMetricModel()
          {
            CreatedTimestamp = DateTime.UtcNow,
            DateLastModified = DateTime.UtcNow,
            UserId = caller.Id,
            MetricTabId = tabId,
            ScoreId = metricId,
            Color = color
          };

          s.Save(metric);
          var updates = new ITrackedMetricHookUpdates();

          await HooksRegistry.Each<IMetricHook>((sess, x) => x.UpdateTrackedMetric(sess, caller, metric, updates));

          tx.Commit();
          s.Flush();

          return metric.Id;
        }
      }
    }

    public static async Task<long> EditMetricTab(UserOrganizationModel caller, long tabId, string title, UnitType? units, Frequency? frequency, long? meetingId, bool? shareToMeeting)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = MetricTabPermissions.Create(s, caller, tabId);
          perms.ViewUserOrganization(caller.Id, false);

          var tab = perms.CanEditMetricTab().ResourceModel;

          if (title != null) tab.Title = title;
          if (units != null) tab.Units = (UnitType)units;
          if (frequency != null) tab.Frequency = (Frequency)frequency;

          if (shareToMeeting != null)
          {
            tab.ShareToMeeting = (bool)shareToMeeting;

            if (!shareToMeeting.Value)
            {
              var tabOwner = UserAccessor.GetUserOrganization(caller, tab.UserId, false, false);
              await SetPinMetricTab(tabOwner, tab.Id, true);
            }
          }

          if (meetingId != null)
          {
            perms.EditL10Recurrence((long) meetingId);
            tab.MeetingId = meetingId;
          }

          s.Update(tab);

          var metricTabHookUpdates = new IMetricTabHookUpdates();
          await HooksRegistry.Each<IMetricHook>((sess, x) => x.UpdateMetricTab(sess, caller, tab, metricTabHookUpdates));

          tx.Commit();
          s.Flush();

          return tab.Id;
        }
      }
    }

    public static async Task<long> DeleteMetricTab(UserOrganizationModel caller, long tabId, long userId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = MetricTabPermissions.Create(s, caller, tabId);
          perms.ViewUserOrganization(caller.Id, false);
          var tab = perms.CanEditMetricTab().ResourceModel;

          tab.DeleteTime = DateTime.UtcNow;

          s.Update(tab);

          var metricTabHookUpdates = new IMetricTabHookUpdates();
          await HooksRegistry.Each<IMetricHook>((sess, x) => x.DeleteMetricTab(sess, caller, tab, metricTabHookUpdates));

          tx.Commit();
          s.Flush();

          return tab.Id;
        }
      }
    }

    public static List<MetricTabModel> GetMetricTabsForUser(UserOrganizationModel caller, long forUserId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          PermissionsUtility.Create(s, caller)
            .ViewUserOrganization(forUserId, true); //may be too restricted?

          return s.QueryOver<MetricTabModel>()
            .Where(x => x.UserId == forUserId && x.DeleteTime == null)
            .List().ToList();
        }
      }
    }

    public static List<MetricTabModel> GetMetricTabsForMeeting(UserOrganizationModel caller, long meetingId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          PermissionsUtility.Create(s, caller)
            .ViewL10Recurrence(meetingId);

          return s.QueryOver<MetricTabModel>()
            .Where(x => x.MeetingId == meetingId && x.DeleteTime == null)
            .Where(x => x.ShareToMeeting == true || x.UserId == caller.Id)
            .List().ToList();
        }
      }
    }

    public static MetricTabModel GetMetricTab(UserOrganizationModel caller, long id)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
          var perms = MetricTabPermissions.Create(s, caller, id);
          var result = perms.CanViewMetricTab().ResourceModel;
          return result;
      }
    }

    public static List<TrackedMetricModel> GetTrackedMetricsForTab(UserOrganizationModel caller, long metricTabId, long forUserId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          PermissionsUtility.Create(s, caller)
            .ViewUserOrganization(forUserId, true); //may be too restricted?

          return s.QueryOver<TrackedMetricModel>()
            .Where(x => x.MetricTabId == metricTabId && x.DeleteTime == null)
            .List().ToList();
        }
      }
    }

    public static List<TrackedMetricModel> GetTrackedMetricsForTab(UserOrganizationModel caller, List<long> metricTabIds, long forUserId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        PermissionsUtility.Create(s, caller)
          .ViewUserOrganization(forUserId, true); //may be too restricted?

        return s.QueryOver<TrackedMetricModel>()
          .Where(x => x.DeleteTime == null)
          .WhereRestrictionOn(x => x.MetricTabId).IsIn(metricTabIds)
          .List().ToList();
      }
    }

    public static List<TrackedMetricModel> GetTrackedMetricsByMetricId(UserOrganizationModel caller, long metricId)
    {
      using(var s = HibernateSession.GetCurrentSession())
      {
        using(var tx = s.BeginTransaction())
        {
          PermissionsUtility.Create(s, caller)
            .ViewUserOrganization(caller.Id, true); //may be too restricted?

          return s.QueryOver<TrackedMetricModel>()
            .Where(x => x.ScoreId == metricId && x.DeleteTime == null)
            .List().ToList();
        }
      }
    }

    public static async Task<long> RemoveMetricFromTab(UserOrganizationModel caller, long userId, long trackedMetricId)
    {
      using var s = HibernateSession.GetCurrentSession();
      using var transaction = s.BeginTransaction();

      var trackedMetricPerms = TrackedMetricPermissions.Create(s, caller, trackedMetricId);
      var trackedMetric = trackedMetricPerms.CanRemoveMetricFromTab().ResourceModel;

      trackedMetric.DeleteTime = DateTime.UtcNow;
      var updates = new ITrackedMetricHookUpdates
      {
        Deleted = true
      };

      s.Update(trackedMetric);

      await HooksRegistry.Each<IMetricHook>((sess, x) => x.UpdateTrackedMetric(sess, caller, trackedMetric, updates));

      transaction.Commit();
      s.Flush();

      return trackedMetric.Id;
    }

    public static async Task<long> RemoveMetricFromTabUnsafe(UserOrganizationModel caller, long userId, long trackedMetricId)
    {
      using var s = HibernateSession.GetCurrentSession();
      using var transaction = s.BeginTransaction();

      var trackedMetric = s.QueryOver<TrackedMetricModel>().Where(m => m.Id == trackedMetricId).SingleOrDefault();

      trackedMetric.DeleteTime = DateTime.UtcNow;
      var updates = new ITrackedMetricHookUpdates
      {
        Deleted = true
      };

      s.Update(trackedMetric);

      await HooksRegistry.Each<IMetricHook>((sess, x) => x.UpdateTrackedMetric(sess, caller, trackedMetric, updates));

      transaction.Commit();
      s.Flush();

      return trackedMetric.Id;
    }

    public static async Task<long> SetPinMetricTab(UserOrganizationModel caller, long metricTabId, bool pinMetricTab)
    {
      using var session = HibernateSession.GetCurrentSession();

      var perms = MetricTabPermissions.Create(session, caller, metricTabId);
      perms.CanPinMetricTab();

      var tab = perms.ResourceModel;
      var metricTabHookUpdates = new IMetricTabHookUpdates();



      await MetricsTabPinnedAccessor.SetPinnedForMetric(caller, metricTabId, pinMetricTab);
      await HooksRegistry.Each<IMetricHook>((sess, x) => x.PinUnpinMetricTab(sess, caller, tab, metricTabHookUpdates));

      return metricTabId;
    }

    public static async Task<List<long>> GetMetricTabIdsByMetricIdUnsafe(long metricId)
    {
      using var session = HibernateSession.GetCurrentSession();

      var metricTabsQuery = session.QueryOver<TrackedMetricModel>()
        .Where(tm => tm.ScoreId == metricId && tm.DeleteTime == null)
        .Select(Projections.Group<TrackedMetricModel>(tm => tm.MetricTabId))
        .List<long>();

      return metricTabsQuery.ToList();
    }

    #endregion

  }
}