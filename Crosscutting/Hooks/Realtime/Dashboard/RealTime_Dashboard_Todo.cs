using RadialReview.Utilities.Hooks;
using System.Collections.Generic;
using NHibernate;
using RadialReview.Models.Todo;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Models;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Base;
using RadialReview.Crosscutting.Hooks.Realtime.L10;
using log4net;
using RadialReview.Utilities.RealTime;

namespace RadialReview.Crosscutting.Hooks.Realtime {
	public class RealTime_Dashboard_Todo : ITodoHook {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public bool CanRunRemotely() {
			return false;
		}
		public bool AbsorbErrors() {
			return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task CreateTodo(ISession s, UserOrganizationModel caller, TodoModel todo) {
			await using (var rt = RealTimeUtility.Create()) {
				rt.UpdateUsers(todo.AccountableUserId)
				  .Update(new ListDataVM(todo.AccountableUserId) {
					Todos = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularTodo(todo))
				  });


				if (todo.ForRecurrenceId > 0) {
					var recurrenceId = todo.ForRecurrenceId.Value;
					RealTimeHelpers.DoRecurrenceUpdate(rt, s, recurrenceId, x => {
						x.Update(new AngularTileId<IEnumerable<AngularTodo>>(0, recurrenceId, null, AngularTileKeys.L10TodoList(recurrenceId)) {
							Contents = AngularList.CreateFrom(AngularListType.Add, new AngularTodo(todo))
						});
					});
				}
			}			
		}

		public async Task UpdateTodo(ISession s, UserOrganizationModel caller, TodoModel todo, ITodoHookUpdates updates) {
			//Updated in other real-time class...
			var _nil = nameof(RealTime_L10_Todo);

		}
	}
}
