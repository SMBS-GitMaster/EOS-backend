using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using System.Threading.Tasks;
using NHibernate;

namespace RadialReview.Accessors
{
  public class MeetingSettingsAccessor
  {

    #region Public Methods

    public static long AddSettings(UserOrganizationModel caller, long recurrenceId, int metricTableWidthDragScrollPct = 60, bool goalVisible = true, bool cumulativeVisible = true, bool averageVisible = true)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller)
            .ViewL10Recurrence(recurrenceId);

          var settings = new MeetingSettingsModel()
          {
            LastViewedTimestamp = DateTime.Now.ToUnixTimeStamp(),
            UserId = caller.Id,
            RecurrenceId = recurrenceId,
            MetricTableWidthDragScrollPct = metricTableWidthDragScrollPct,
            GoalVisible = goalVisible,
            CumulativeVisible = cumulativeVisible,
            AverageVisible = averageVisible,
          };
          s.Save(settings);

          tx.Commit();
          s.Flush();

          return settings.Id;
        }
      }
    }

    public static long EditSettings(UserOrganizationModel caller, long settingsId, double? lastViewed = null, int? metricTableWidthDragScrollPct = null, bool? goalVisible = null, bool? cumulativeVisible = null, bool? averageVisible = null)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {

          var settings = s.Get<MeetingSettingsModel>(settingsId);
          var perms = PermissionsUtility.Create(s, caller)
            .ViewL10Recurrence(settings.RecurrenceId);

          if(lastViewed != null)
          {
            settings.LastViewedTimestamp = lastViewed;
          }

          if(metricTableWidthDragScrollPct != null)
          {
            settings.MetricTableWidthDragScrollPct = (int)metricTableWidthDragScrollPct;
          }

          if(goalVisible != null)
          {
            settings.GoalVisible = (bool)goalVisible;
          }

          if(cumulativeVisible != null)
          {
            settings.CumulativeVisible = (bool)cumulativeVisible;
          }

          if(averageVisible != null)
          {
            settings.AverageVisible = (bool)averageVisible;
          }


          s.Update(settings);

          tx.Commit();
          s.Flush();

          return settings.Id;
        }
      }
    }

    public static MeetingSettingsModel GetSettings(UserOrganizationModel caller, long settingsId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var settings = s.Get<MeetingSettingsModel>(settingsId);
          var perms = PermissionsUtility.Create(s, caller)
            .ViewL10Recurrence(settings.RecurrenceId);

          return settings;
        }
      }
    }

    public static MeetingSettingsModel GetSettingsForMeeting(UserOrganizationModel caller, long recurrenceId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller)
            .ViewL10Recurrence(recurrenceId);

          MeetingSettingsModel result = s.QueryOver<MeetingSettingsModel>()
              .Where(x => x.UserId == caller.Id && x.RecurrenceId == recurrenceId)
              .List().FirstOrDefault();

          if(result == null)
          {
            // Auto create
            long settingsId = AddSettings(caller, recurrenceId);
            return GetSettings(caller, settingsId);
          }
          else
          {
            return result;
          }
        }
      }
    }

    public static List<MeetingSettingsModel> GetSettingsForMeetings(ISession s, UserOrganizationModel caller,  List<long> recurrenceIds)
    {        
        List<MeetingSettingsModel> results = s.QueryOver<MeetingSettingsModel>()
            .Where(x => x.UserId == caller.Id)
            .WhereRestrictionOn(x => x.RecurrenceId).IsIn(recurrenceIds)
            .List().ToList();
        List<long> recurreceIdsWithNoSettings = recurrenceIds.Where(x => !results.Any(y => y.RecurrenceId == x)).ToList();
        foreach(var recurrenceId in recurreceIdsWithNoSettings)
        {
            // Auto create
            long settingsId = AddSettings(caller, recurrenceId);
            MeetingSettingsModel setting = GetSettings(caller, settingsId);
            results.Add(setting);
        }
        return results;
    }

    public static MeetingSettingsModel GetSettingsForMeeting_unsafe(UserOrganizationModel caller, long recurrenceId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          MeetingSettingsModel result = s.QueryOver<MeetingSettingsModel>()
              .Where(x => x.UserId == caller.Id && x.RecurrenceId == recurrenceId)
              .List().FirstOrDefault();

          if (result == null)
          {
            // Auto create
            long settingsId = AddSettings(caller, recurrenceId);
            return GetSettings(caller, settingsId);
          }
          else
          {
            return result;
          }
        }
      }
    }

    #endregion

  }
}
