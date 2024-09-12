using Hangfire;
using RadialReview.Accessors;
using RadialReview.Crosscutting.CronJobs;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.CronJobs {

  /// <summary>
  /// Hourly
  /// </summary>
  public class HourlyEventAnalyzerEventsCronJob : ICronJob {
    public CronJobBehavior Behavior => new CronJobBehavior(true, "HourlyEventAnalyzerEventsCronJob_v1", Cron.Hourly(6), GapExecutionBehavior.FillInGaps);

    public async Task Execute(DateTime executeTime) {
      Scheduler.Enqueue(() => EventAccessor.ExecuteAll_Hangfire(EventFrequency.Hourly, executeTime));
    }
  }

  /// <summary>
  /// Daily
  /// </summary>
  public class DailyEventAnalyzerEventsCronJob : ICronJob {
    public CronJobBehavior Behavior => new CronJobBehavior(true, "DailyEventAnalyzerEventsCronJob_v1", Cron.Daily(2, 12), GapExecutionBehavior.FillInGaps);

    public async Task Execute(DateTime executeTime) {
      Scheduler.Enqueue(() => EventAccessor.ExecuteAll_Hangfire(EventFrequency.Daily, executeTime));
    }
  }

  /// <summary>
  /// Weekly
  /// </summary>
  public class WeeklyEventAnalyzerEventsCronJob : ICronJob {
    public CronJobBehavior Behavior => new CronJobBehavior(true, "WeeklyEventAnalyzerEventsCronJob_v1", Cron.Weekly(DayOfWeek.Sunday, 2), GapExecutionBehavior.FillInGaps);

    public async Task Execute(DateTime executeTime) {
      Scheduler.Enqueue(() => EventAccessor.ExecuteAll_Hangfire(EventFrequency.Weekly, executeTime));
    }
  }

  /// <summary>
  /// biweekly
  /// </summary>
  public class BiweeklyEventAnalyzerEventsCronJob : ICronJob {
    public CronJobBehavior Behavior => new CronJobBehavior(true, "BiweeklyEventAnalyzerEventsCronJob_v1", Cron.Weekly(DayOfWeek.Sunday, 2, 6), GapExecutionBehavior.FillInGaps);

    public async Task Execute(DateTime executeTime) {
      var weekOfYear = (int)(executeTime.DayOfYear/52);
      if (weekOfYear % 2 == 0) {
        Scheduler.Enqueue(() => EventAccessor.ExecuteAll_Hangfire(EventFrequency.Biweekly, executeTime));
      }
    }
  }

  /// <summary>
  /// Monthly
  /// </summary>
  public class MonthlyEventAnalyzerEventsCronJob : ICronJob {
    public CronJobBehavior Behavior => new CronJobBehavior(true, "MonthlyEventAnalyzerEventsCronJob_v1", Cron.Monthly(1, 2, 18), GapExecutionBehavior.FillInGaps);

    public async Task Execute(DateTime executeTime) {
      Scheduler.Enqueue(() => EventAccessor.ExecuteAll_Hangfire(EventFrequency.Monthly, executeTime));
    }
  }

  /// <summary>
  /// Quarterly
  /// </summary>
  public class QuarterlyEventAnalyzerEventsCronJob : ICronJob {
    public CronJobBehavior Behavior => new CronJobBehavior(true, "QuarterlyEventAnalyzerEventsCronJob_v1", Cron.Monthly(1, 2, 24), GapExecutionBehavior.FillInGaps);

    public async Task Execute(DateTime executeTime) {
      var monthOfYear = (int)(executeTime.Month);
      if (monthOfYear % 4 == 0) {
        Scheduler.Enqueue(() => EventAccessor.ExecuteAll_Hangfire(EventFrequency.Quarterly, executeTime));
      }
    }
  }

  /// <summary>
  /// Yearly
  /// </summary>
  public class YearlyEventAnalyzerEventsCronJob : ICronJob {
    public CronJobBehavior Behavior => new CronJobBehavior(true, "YearlyEventAnalyzerEventsCronJob_v1", Cron.Yearly(1, 1, 2, 33), GapExecutionBehavior.FillInGaps);

    public async Task Execute(DateTime executeTime) {
      Scheduler.Enqueue(() => EventAccessor.ExecuteAll_Hangfire(EventFrequency.Yearly, executeTime));
    }
  }

}
