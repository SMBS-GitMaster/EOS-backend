namespace RadialReview.Accessors
{
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
  using RadialReview.GraphQL.Models;
  using UserOrganizationModel = Models.UserOrganizationModel;

  public class MetricsTabPinnedAccessor : BaseAccessor
  {

    #region Public Methods

    public static MetricsTabPinnedModel GetMetricTabPinned(UserOrganizationModel caller, long id)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          MetricsTabPinnedModel result = s.QueryOver<MetricsTabPinnedModel>().Where(x => x.Id == id).List().FirstOrDefault();
          if (result != null && result.UserId != caller.Id)
          {
            throw new PermissionsException();
          }
          return result;
        }
      }
    }

    public static MetricsTabPinnedModel GetPinnedForMetric(UserOrganizationModel caller, long metricTabId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          MetricsTabPinnedModel result = s.QueryOver<MetricsTabPinnedModel>()
            .Where(x => x.MetricsTabId == metricTabId && x.UserId == caller.Id).List().FirstOrDefault();
          return result;
        }
      }
    }

    public static Task<long> SetPinnedForMetric(UserOrganizationModel caller, long metricTabId, bool pinned)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {

          MetricsTabPinnedModel entry = s.QueryOver<MetricsTabPinnedModel>()
            .Where(x => x.MetricsTabId == metricTabId && x.UserId == caller.Id).List().FirstOrDefault();

          if (entry == null)
          {
            // Create new
            entry = new MetricsTabPinnedModel
            {
              CreatedTimestamp = DateTime.UtcNow,
              IsPinnedToTabBar = pinned,
              MetricsTabId = metricTabId,
              UserId = caller.Id,
            };
          }
          else
          {
            // Update
            entry.IsPinnedToTabBar = pinned;
          }
          entry.DateLastModified = DateTime.UtcNow;

          s.Save(entry);
          tx.Commit();
          s.Flush();

          return Task.FromResult(entry.Id);
        }
      }
    }

    #endregion

  }

}