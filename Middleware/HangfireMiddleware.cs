using Hangfire;
using Hangfire.Console;
using Hangfire.Pro.Redis;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RadialReview.Hangfire;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hangfire.Server;
using RadialReview.Hangfire.Activator;
using RadialReview.Utilities.Logging;
using RadialReview.Crosscutting.Hangfire.Filters;
using RadialReview.Crosscutting.Hangfire.Debounce;

namespace RadialReview.Middleware {
	public static class HangfireMiddleware {
		public static void ConfigureHangfire(this IServiceCollection services) {
			services.AddHangfire((sp, configuration) => {
				var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
				configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
					.UseSimpleAssemblyNameTypeSerializer()
					.UseRecommendedSerializerSettings()
					.UseConsole()
					.UseBatches()
					.UseRedisStorage(
						Config.GetHangfireConnectionString(),
						new RedisStorageOptions() { InvisibilityTimeout = TimeSpan.FromHours(3) }
					).UseFilter(new AutomaticRetryAttribute { Attempts = 0 })
					.UseFilter(new ProlongExpirationTimeAttribute())
					.UseActivator(new ServiceScopeActivator(scopeFactory));

			});

			// Add the processing server as IHostedService
			var delayedScheduler = new DelayedJobQueueScheduler();
			delayedScheduler.UseBackgroundPool(1);
			services.AddSingleton<IBackgroundProcess>(delayedScheduler);

			services.AddHangfireServer(x => {
				x.Queues = GetAvailableQueues().Distinct().ToArray();
				x.ServerName = Guid.NewGuid().ToString();
			});


			if (Config.IsHangfireWorker()) {
				MachineId.AddMachineType(MachineType.Hangfire);
			}


		}

		public static void ConfigureHangfire(this IEndpointRouteBuilder app) {
      app.MapHangfireDashboard("/hangfire", new DashboardOptions
      {
        Authorization = new[] {
          new HangfireAuth()
        },
      });
		}


		private static string[] GetAvailableQueues() {
			var awsEnv = "awsenv_" + (new Regex("[^a-zA-Z0-9]").Replace(Config.GetAwsEnv(), ""));

			var myQueues = new List<string> {
				awsEnv,
				HangfireQueues.DEFAULT,
				HangfireQueues.Immediate.MIGRATION
			};

			if (Config.IsHangfireWorker()) {
				myQueues = new List<string>();
				myQueues.Add(awsEnv);
				myQueues.AddRange(HangfireQueues.OrderedQueues.Where(x => x != HangfireQueues.Immediate.ALPHA));
			}

			if (Config.IsDefinitelyAlpha() || Config.IsLocal() || Config.IsDefinitelyQa()) {
				myQueues.Add(HangfireQueues.Immediate.ALPHA);
			}

			if (Config.IsLocal() || Config.IsStaging()) {
				myQueues.Add(HangfireQueues.Immediate.MIGRATION);
			}

			return myQueues.ToArray();

		}
	}
}
