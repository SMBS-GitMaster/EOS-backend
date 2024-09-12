using RadialReview.Crosscutting.Hooks;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NHibernate;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System.Linq.Expressions;

namespace RadialReview.Middleware.BackgroundServices {

  public class HookExecutionService : BackgroundService {
    private static TimeSpan STAT_RANGE = TimeSpan.FromMinutes(60);
    private ILogger<HookExecutionService> _logger;
    private static QueueRunningStats _successStats = new QueueRunningStats(STAT_RANGE);
    private static QueueRunningStats _failStats = new QueueRunningStats(STAT_RANGE);
    private static ConcurrentQueue<QueueTaskContext> TaskContextQueue = new ConcurrentQueue<QueueTaskContext>();


    #region Helper Classes
    public class QueueTaskContext {
      public QueueTaskContext(Func<ISession, ITransaction, Task> executable, ReadOnlyHookData hookData) {
        Executable = executable;
        HookData = hookData;
      }
      public Func<ISession, ITransaction, Task> Executable { get; set; }
      public ReadOnlyHookData HookData { get; set; }
    }
    #endregion


    public HookExecutionService(ILogger<HookExecutionService> logger) {
      _logger = logger;
    }

    public static QueueRunningStats GetSuccessStats() {
      return _successStats;
    }
    public static QueueRunningStats GetFailStats() {
      return _failStats;
    }
    public static void RunAsync(Func<ISession, ITransaction, Task> func) {
      var a = HookData.ToReadOnly();
      RunAsync(a, func);
    }
    public static void RunAsync(ReadOnlyHookData hookData, Func<ISession, ITransaction, Task> func) {
      var a = HookData.ToReadOnly();
      Enqueue(func, hookData);
    }
    protected static void Enqueue(Func<ISession, ITransaction, Task> task, ReadOnlyHookData hookData) {
      TaskContextQueue.Enqueue(new QueueTaskContext(task, hookData));
    }

    protected override async Task ExecuteAsync(CancellationToken stopToken) {
      do {
        try {
          QueueTaskContext ctx;
          while (TaskContextQueue.TryDequeue(out ctx)) {
            HookData.LoadFrom(ctx.HookData);
            var g = (Guid.NewGuid() + "").Substring(0, 6);
            Debug.BeginStack("Beginning " + g);
            using (var s = HibernateSession.CreateOuterSession()) {
              using (var tx = s.BeginTransaction()) {
                await ctx.Executable(s, tx);
              }
            }
            Debug.EndStack("Ending " + g);
            _successStats.LogAction();
          }
        } catch (Exception e) {
          //hmm
          _logger.LogError("Hook Execution Failed", e);
          _failStats.LogAction();
        }
        await Task.Delay(50);
        _successStats.Clean();
        _failStats.Clean();
      } while (!stopToken.IsCancellationRequested);
    }
  }
}