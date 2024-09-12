using Hangfire;
using RadialReview.Accessors;
using RadialReview.Crosscutting.CronJobs;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.CronJobs {
  public class GenerateAllDailyEventsCronJob : ICronJob {
    public CronJobBehavior Behavior => new CronJobBehavior(true, "GenerateAllDailyEventsCronJob_v1", Cron.Daily(1,6), GapExecutionBehavior.OnlyMostRecent);

    public async Task Execute(DateTime executeTime) {
      await EventUtil.GenerateAllDailyEvents_Hangfire(executeTime);
    }
  }
}
