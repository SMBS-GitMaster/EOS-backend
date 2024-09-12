using Amazon.S3.Model;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Dialect.Function;
using NHibernate.Transform;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Accessors
{
  public class AverageScoreData
  {
    public long MetricId { get; set; }
    public DateTime AverageDateRange { get; set; }
    public decimal AverageScore { get; set; }
  }

  public class CumulativeScoreData
  {
    public long MetricId { get; set; }
    public DateTime CumulativeDateRange { get; set; }
    public decimal CumulativeScore { get; set; }
  }

  public class ProgressiveScoreData
  {
    public long MetricId { get; set; }
    public DateTime ProgressiveDateRange { get; set; }
    public decimal ProgressiveScore { get; set; }
  }
  public class MetricAccessor
  {
    /// <summary>
    /// Initializes metrics to zero.
    /// </summary>
    /// <remarks>
    /// This method prepares a dictionary of metrics to be displayed based on the configured indicators for each metric,
    /// including ShowAverage, ShowCumulative, and the presence of ProgressiveData. Each metric identified by these criteria
    /// is initialized with zero values for their corresponding data components (AverageData, CumulativeData, ProgressiveData).
    /// This initialization ensures that all relevant metrics are included in the result set with an initial value of zero,
    /// allowing them to be subsequently updated with actual data through further queries. If no additional data is found in these queries,
    /// the metric values will remain at zero, indicating that there was no activity or data to sum during the queried period.
    /// This approach guarantees that all metrics configured to be displayed are included in reports, even those without recent data.
    /// </remarks>
    public static Dictionary<long, MetricDataModel> InitializeMetricDataDict(ISession session, IReadOnlyList<long> metricIds)
    {
      var metricsToDisplay = session.Query<MeasurableModel>()
          .Where(m => metricIds.Contains(m.Id) &&
                      (m.ShowAverage || m.ShowCumulative || m.ProgressiveDate != null))
          .Select(m => new
          {
            m.Id,
            m.ShowAverage,
            m.AverageRange,
            m.ShowCumulative,
            m.CumulativeRange,
            HasProgressiveData = (m.ProgressiveDate != null),
            m.ProgressiveDate
          })
          .ToList();

      var metricDataDict = new Dictionary<long, MetricDataModel>();
      foreach (var metric in metricsToDisplay)
      {
        var metricData = new MetricDataModel();

        if (metric.ShowAverage)
        {
          metricData.AverageData = new MetricAverageDataModel { StartDate = metric.AverageRange.ToUnixTimeStamp(), Average = 0 };
        }
        if (metric.ShowCumulative)
        {
          metricData.CumulativeData = new MetricCumulativeDataModel { StartDate = metric.CumulativeRange.ToUnixTimeStamp(), Sum = 0 };
        }
        if (metric.HasProgressiveData)
        {
          metricData.ProgressiveData = new MetricProgressiveDataModel { TargetDate = metric.ProgressiveDate.ToUnixTimeStamp(), Sum = 0 };
        }

        metricDataDict[metric.Id] = metricData;
      }

      return metricDataDict;
    }
    public static IList<AverageScoreData> GetScoreAverageDataByMetricIds(IReadOnlyList<long> metricIds, UserOrganizationModel caller, DayOfWeek? meetingStartWeek)
    {
      using var session = HibernateSession.GetCurrentSession();
      return GetScoreAverageDataByMetricIds(session, metricIds, caller, meetingStartWeek);
    }

    public static IList<AverageScoreData> GetScoreAverageDataByMetricIds(ISession session, IReadOnlyList<long> metricIds, UserOrganizationModel caller, DayOfWeek? meetingStartWeek)
    {
      var startOfWeek = meetingStartWeek ?? caller.GetOrganizationSettings().WeekStart;
      var endDate = DateTime.Today.StartOfWeek(startOfWeek);

      var averageResults = session.Query<ScoreModel>()
       .Where(s => metricIds.Contains(s.Measurable.Id) &&
                   s.Measurable.ShowAverage &&
                   s.Measurable.Frequency != Frequency.DAILY &&
                   s.Measured.HasValue)
       .ToList()
       .Where(s => (getCalculatedWeek(startOfWeek, s.ForWeek, s.Measurable.Frequency) >= s.Measurable.AverageRange.Value.StartOfWeek(startOfWeek))
              && getCalculatedWeek(startOfWeek, s.ForWeek, s.Measurable.Frequency).StartOfWeek(startOfWeek) <= endDate)
       .Select(s => new
       {
         MeasurableId = s.Measurable.Id,
         MeasurableAverageRange = s.Measurable.AverageRange.Value,
         Measured = s.Measured.Value
       })
       .ToList()
       .GroupBy(s => s.MeasurableId)
       .Select(g => new AverageScoreData
       {
         MetricId = g.Key,
         AverageScore = g.Average(x => x.Measured),
         AverageDateRange = g.First().MeasurableAverageRange
       })
       .ToList();

      var dailyAverageResults = GetDailyScoreAverageDataByMetricIds(session, metricIds);

      return averageResults
        .Concat(dailyAverageResults)
        .ToList();
    }

    public static IList<CumulativeScoreData> GetScoreCumulativeDataByMetricIds(IReadOnlyList<long> metricIds, UserOrganizationModel caller, DayOfWeek? meetingStartWeek)
    {
      using var session = HibernateSession.GetCurrentSession();
      return GetScoreCumulativeDataByMetricIds(session, metricIds, caller, meetingStartWeek);
    }

    public static DateTime getCalculatedWeek(DayOfWeek startWeek, DateTime forWeek, Frequency frequency)
    {
      return frequency == Frequency.WEEKLY && startWeek == DayOfWeek.Sunday ? forWeek.AddDays(-7) : forWeek;
    }

    public static IList<CumulativeScoreData> GetScoreCumulativeDataByMetricIds(ISession session, IReadOnlyList<long> metricIds, UserOrganizationModel caller, DayOfWeek? meetingStartWeek)
    {
      var startOfWeek = meetingStartWeek ?? caller.GetOrganizationSettings().WeekStart;
      var endDate = DateTime.Today.StartOfWeek(startOfWeek);

      var cumulativeResults = session.Query<ScoreModel>()
      .Where(s => metricIds.Contains(s.Measurable.Id) &&
                  s.Measurable.ShowCumulative &&
                  s.Measurable.Frequency != RadialReview.Models.Enums.Frequency.DAILY &&
                  s.Measured.HasValue
      ).ToList()
      .Where(s => (getCalculatedWeek(startOfWeek, s.ForWeek, s.Measurable.Frequency) >= s.Measurable.CumulativeRange.Value.StartOfWeek(startOfWeek))
                    && getCalculatedWeek(startOfWeek, s.ForWeek, s.Measurable.Frequency).StartOfWeek(startOfWeek) <= endDate)
      .Select(s => new
      {
        MeasurableId = s.Measurable.Id,
        MeasurableCumulativeRange = s.Measurable.CumulativeRange.Value,
        Measured = s.Measured.Value,

      })
        .ToList()
        .GroupBy(s => s.MeasurableId)
        .Select(g => new CumulativeScoreData
        {
          MetricId = g.Key,
          CumulativeScore = g.Sum(x => x.Measured),
          CumulativeDateRange = g.First().MeasurableCumulativeRange
        })
        .ToList();

      var dailyCumulativeResults = GetDailyCumulativeScoreDataByMetricIds(session, metricIds);
      return cumulativeResults
        .Concat(dailyCumulativeResults)
        .ToList();
    }

    public static IList<ProgressiveScoreData> GetScoreProgressiveDataByMetricIds(ISession session, IReadOnlyList<long> metricIds)
    {
      var results = session.Query<ScoreModel>()
      .Where(s => metricIds.Contains(s.MeasurableId) &&
                  s.Measurable.Frequency != RadialReview.Models.Enums.Frequency.DAILY &&
                  s.Measured.HasValue &&
                  s.Measurable.ProgressiveDate != null &&
                  s.ForWeek <= s.Measurable.ProgressiveDate.Value.AddDays(7))
      .Select(s => new
      {
        MeasurableId = s.Measurable.Id,
        MeasurableProgressiveDate = s.Measurable.ProgressiveDate.Value,
        Measured = s.Measured.Value
      })
      .ToList()
      .GroupBy(s => s.MeasurableId)
      .Select(g => new ProgressiveScoreData
      {
        MetricId = g.Key,
        ProgressiveDateRange = g.First().MeasurableProgressiveDate,
        ProgressiveScore = g.Sum(x => x.Measured)
      })
      .ToList();

      var dailyProgressiveResults = GetDailyScoreProgressiveDataByMetricIds(session, metricIds);

      return results.Concat(dailyProgressiveResults).ToList();
    }

    public static IList<ProgressiveScoreData> GetDailyScoreProgressiveDataByMetricIds(ISession session, IReadOnlyList<long> metricIds)
    {
      var results = session.Query<ScoreModel>()
      .Where(s => metricIds.Contains(s.MeasurableId) &&
                  s.Measurable.Frequency == RadialReview.Models.Enums.Frequency.DAILY &&
                  s.Measured.HasValue &&
                  s.Measurable.ProgressiveDate != null &&
                  s.ForWeek <= s.Measurable.ProgressiveDate.Value.Date)
      .Select(s => new
      {
        MeasurableId = s.Measurable.Id,
        MeasurableProgressiveDate = s.Measurable.ProgressiveDate.Value,
        Measured = s.Measured.Value
      })
      .ToList()
      .GroupBy(s => s.MeasurableId)
      .Select(g => new ProgressiveScoreData
      {
        MetricId = g.Key,
        ProgressiveDateRange = g.First().MeasurableProgressiveDate,
        ProgressiveScore = g.Sum(x => x.Measured)
      })
      .ToList();

      return results;
    }


    public static IList<AverageScoreData> GetDailyScoreAverageDataByMetricIds(ISession session, IReadOnlyList<long> metricIds)
    {
      var endDate = DateTime.UtcNow.Date;

      var averageResults = session.Query<ScoreModel>()
          .Where(s => metricIds.Contains(s.Measurable.Id) &&
                      s.Measurable.Frequency == RadialReview.Models.Enums.Frequency.DAILY &&
                      s.Measurable.ShowAverage &&
                      s.ForWeek.Date >= s.Measurable.AverageRange.Value.Date &&
                      s.ForWeek.Date <= endDate &&
                      s.Measured.HasValue)
        .Select(s => new
        {
          MeasurableId = s.Measurable.Id,
          MeasurableAverageRange = s.Measurable.AverageRange.Value.Date,
          Measured = s.Measured.Value
        })
       .ToList()
       .GroupBy(s => s.MeasurableId)
       .Select(g => new AverageScoreData
       {
         MetricId = g.Key,
         AverageScore = g.Average(x => x.Measured),
         AverageDateRange = g.First().MeasurableAverageRange
       })
       .ToList();

      return averageResults;
    }

    public static IList<CumulativeScoreData> GetDailyCumulativeScoreDataByMetricIds(ISession session, IReadOnlyList<long> metricIds)
    {
      var endDate = DateTime.UtcNow.Date;

      var cumulativeResults = session.Query<ScoreModel>()
        .Where(s => metricIds.Contains(s.Measurable.Id) &&
                    s.Measurable.Frequency == RadialReview.Models.Enums.Frequency.DAILY &&
                    s.Measurable.ShowCumulative &&
                    s.ForWeek.Date >= s.Measurable.CumulativeRange.Value.Date &&
                    s.ForWeek.Date <= endDate &&
                    s.Measured.HasValue)
        .Select(s => new
        {
          MeasurableId = s.Measurable.Id,
          MeasurableCumulativeRange = s.Measurable.CumulativeRange.Value,
          Measured = s.Measured.Value
        })
       .ToList()
       .GroupBy(s => s.MeasurableId)
       .Select(g => new CumulativeScoreData
       {
         MetricId = g.Key,
         CumulativeScore = g.Sum(x => x.Measured),
         CumulativeDateRange = g.First().MeasurableCumulativeRange
       })
       .ToList();

      return cumulativeResults;
    }

  }
}
