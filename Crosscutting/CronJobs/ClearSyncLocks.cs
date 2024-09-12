using Hangfire;
using RadialReview.Accessors;
using RadialReview.Crosscutting.Hangfire.Jobs;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.CronJobs {
  public class ClearSyncLocks : ICronJob {
    public CronJobBehavior Behavior => new CronJobBehavior(true, "ClearSyncLock_v1", Cron.Hourly(36), GapExecutionBehavior.OnlyMostRecent);

    public async Task Execute(DateTime executeTime) {
      var days = 0.1;
      await TaskAccessor.CleanupSyncs_Hangfire(days);
    }
  }
}
