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
  public class TaskSchedulerCronJob : ICronJob {
    public CronJobBehavior Behavior => new CronJobBehavior(true, "TaskSchedulerCronJob_v1", Cron.Minutely(), GapExecutionBehavior.OnlyPast24Hours);

    public async Task Execute(DateTime executeTime) {
      if (executeTime.Minute % 30 == 0) {
        await TaskAccessor.EnqueueTasks(executeTime);
      }
    }
  }
}
