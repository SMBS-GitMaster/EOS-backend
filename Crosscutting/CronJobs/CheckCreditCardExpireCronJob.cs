using Hangfire;
using RadialReview.Accessors;
using RadialReview.Crosscutting.CronJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.CronJobs {
  public class CheckCreditCardExpireCronJob : ICronJob {
    public CronJobBehavior Behavior => new CronJobBehavior(true, "CheckCreditCardExpire_v1", Cron.Daily(1), GapExecutionBehavior.OnlyMostRecent);

    public async Task Execute(DateTime executeTime) {
      await TaskAccessor.CheckCardExpirations_Hangfire();
    }
  }
}
