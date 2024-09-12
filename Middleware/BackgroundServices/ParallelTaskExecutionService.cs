using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Middleware.BackgroundServices {
	public class ParallelTaskExecutionService : BackgroundService {

		private static int MAX_CONCURRENT = 3;
		private static TimeSpan STAT_RANGE = TimeSpan.FromMinutes(60);
		private static ConcurrentBag<Executable> TaskContextBag = new ConcurrentBag<Executable>();
		private static QueueRunningStats _successStats = new QueueRunningStats(STAT_RANGE);
		private static QueueRunningStats _failStats = new QueueRunningStats(STAT_RANGE);

		private ILogger<ParallelTaskExecutionService> _logger;

		public ParallelTaskExecutionService(ILogger<ParallelTaskExecutionService> logger) {
			_logger = logger;
		}

		public static QueueRunningStats GetSuccessStats() {
			return _successStats;
		}
		public static QueueRunningStats GetFailStats() {
			return _failStats;
		}
		protected async Task ExecuteWrapped(Executable task) {
			try {
				await task();
				_successStats.LogAction();
			} catch (Exception e) {
				_failStats.LogAction();
			}
		}

		public delegate Task Executable();

		/// <summary>
		/// All variables from the outer scope must be fully resolved 
		/// </summary>
		/// <param name="task"></param>
		public static void BeginTaskAsync(Executable task) {
			TaskContextBag.Add(task);
		}

		protected override async Task ExecuteAsync(CancellationToken stopToken) {
			do {
				try {
					Executable task;
					var parallelTaskList = new List<Task>();
					int i = 0;
					while (i < Math.Max(1, MAX_CONCURRENT) && TaskContextBag.TryTake(out task)) {
						parallelTaskList.Add(ExecuteWrapped(task));
						i++;
					}
					if (parallelTaskList.Any()) {
						await Task.WhenAll(parallelTaskList);
					}

				} catch (Exception e) {
					_logger.LogError("Fatal Parallel Execution Error", e);
				}
				await Task.Delay(100);
				_successStats.Clean();
				_failStats.Clean();


			} while (!stopToken.IsCancellationRequested);
		}
	}
}
