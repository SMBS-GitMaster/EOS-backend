using log4net;
using Microsoft.AspNetCore.SignalR;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Utilities.RealTime {
	public partial class RealTimeUtility : IAsyncDisposable {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private static IHubContext<RealTimeHub> _realTimeHubContext { get; set; }
		public static void Configure(IServiceProvider service) {
			_realTimeHubContext = service.GetService(typeof(IHubContext<RealTimeHub>)) as IHubContext<RealTimeHub>;
			Debug.WriteLine("RealTimeUtility configured.");
			//Register other hubs here. Also update GetHubContext method
		}


		private Dictionary<UpdaterSettings, AngularUpdate> _updaters = new Dictionary<UpdaterSettings, AngularUpdate>();
		private Dictionary<UpdaterSettings, IClientProxy> _proxies = new Dictionary<UpdaterSettings, IClientProxy>();
		private List<Action> _actions = new List<Action>();
		private bool _executed = false;
		private bool _skipExecution = false;
		private string _skipUser = null;

		public bool ExecuteOnException { get; set; }
		private RealTimeUtility() {
			if (ExceptionUtility.IsInException()) {
				//we started this request in an exception, we likely want this to run.
				ExecuteOnException = true;
			}
		}

		private RealTimeUtility(string skipUser, bool shouldExecute) : this() {
			// TODO: Complete member initialization
			_skipExecution = !shouldExecute;
			_skipUser = skipUser;
		}

		public static RealTimeUtility Create() {
			return new RealTimeUtility(null, true);
		}
		public static RealTimeUtility Create(bool shouldExecute = true) {
			return new RealTimeUtility(null, shouldExecute);
		}
		public static RealTimeUtility Create(string skipUser = null, bool shouldExecute = true) {
			return new RealTimeUtility(skipUser, shouldExecute);
		}

		public void SetSkipUser(string skipUser) {
			_skipUser = skipUser;
		}

		public void DoNotExecute() {
			if (_executed)
				throw new PermissionsException("Already executed.");
			_skipExecution = true;
		}


		public RecurrenceUpdater UpdateRecurrences(params long[] recurrences) {
			return new RecurrenceUpdater(recurrences, this);
		}
		public RecurrenceUpdater UpdateRecurrences(IEnumerable<long> recurrences) {
			return UpdateRecurrences(recurrences.ToArray());
		}
		public VtoUpdater UpdateVtos(IEnumerable<long> vtos) {
			return UpdateVtos(vtos.ToArray());
		}
		public VtoUpdater UpdateVtos(params long[] vtos) {
			return new VtoUpdater(vtos, this);
		}
		public OrganizationUpdater UpdateOrganization(long orgId) {
			return new OrganizationUpdater(orgId, this);
		}
		public IBaseUpdater UpdateCaller(UserOrganizationModel caller) {
			if (caller == null)
				return new NoopUpdater(this);
			if (!string.IsNullOrWhiteSpace(caller.GetConnectionId()))
				return UpdateConnection(caller.GetConnectionId());
			return UpdateUsers(caller.Id);
		}
		public IBaseUpdater UpdateCaller(PermissionsUtility perms) {
			return UpdateCaller(perms.GetCaller());
		}

		public UserIdUpdater UpdateUsers(params long[] userIds) {
			return new UserIdUpdater(userIds, this);
		}
		public UserEmailUpdater UpdateUsers(params string[] emails) {
			return new UserEmailUpdater(emails, this);
		}
		public GroupUpdater UpdateGroup(string group) {
			return new GroupUpdater(group.AsList(), this);
		}
		public GroupUpdater UpdateGroups(IEnumerable<string> groups) {
			return new GroupUpdater(groups, this);
		}
		public ConnectionIdUpdater UpdateConnection(string connectionId) {
			return new ConnectionIdUpdater(connectionId.AsList(), this);
		}

		public EveryoneUpdater UpdateEveryone() {
			return new EveryoneUpdater(this);
		}

		protected static IHubContext<HUB> GetHubContext<HUB>() where HUB : Hub {
			if (typeof(HUB) == typeof(RealTimeHub))
				return (IHubContext<HUB>)_realTimeHubContext;
			throw new NotImplementedException("Unregistered Hub:" + typeof(HUB).Name);
		}

		private IReadOnlyList<string> GetExcludedUsers(UpdaterSettings key) {
			if (key.ApplySkip && _skipUser != null) {
				return (new[] { _skipUser }).ToList().AsReadOnly();
			}
			return (new string[0]).ToList().AsReadOnly();
		}

		protected void AddAction(Action a, int insert = int.MaxValue) {
			insert = Math.Min(insert, _actions.Count());
			_actions.Insert(insert, a);
		}

		protected async Task<bool> Execute() {
			if (_skipExecution) {
				return false;
			}
			if (_executed) {
				throw new PermissionsException("Cannot execute again.");
			}
			_executed = true;
			_actions.ForEach(f => {
				try {
					f();
				} catch (Exception e) {
					log.Error("RealTime exception", e);
				}
			});

			foreach (var b in _updaters) {
				try {
					var group = _proxies[b.Key];
					var angularUpdate = b.Value;
					await group.SendAsync("update", angularUpdate);
				} catch (Exception e) {
					log.Error("SignalR exception", e);
				}
			}

			return true;
		}

		public async ValueTask DisposeAsync() {
			if (!_executed) {
				if (ExecuteOnException || !ExceptionUtility.IsInException()) {
					//!! probably should be awaited... Done.
					await Execute();
				} else {
					log.Info("skipping real-time execute due to exception");
				}
			}
		}
		public async Task AddToGroup(string connectionId, string groupName) {
			await _realTimeHubContext.Groups.AddToGroupAsync(connectionId, groupName);
		}
	}

	public static class RealTimeExtensions {
		public static StatusUpdater GetStatusUpdater(this RealTimeUtility.IBaseUpdater updater) {
			return new StatusUpdater(updater);
		}
	}
}
