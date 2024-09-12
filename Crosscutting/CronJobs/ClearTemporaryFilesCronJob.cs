using Hangfire;
using RadialReview.Accessors;
using RadialReview.Crosscutting.CronJobs;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Middleware.Services.BlobStorageProvider;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.CronJobs {
  public class ClearTemporaryFilesCronJob : ICronJob {
    public CronJobBehavior Behavior => new CronJobBehavior(true, "ClearTemporaryFilesCronJob_v1", Cron.Daily(1, 6), GapExecutionBehavior.OnlyMostRecent);

    public async Task Execute(DateTime executeTime) {
      Scheduler.Enqueue(() => FileAccessor.ClearTemporaryFiles(executeTime, default(IBlobStorageProvider)));
    }
  }
}
