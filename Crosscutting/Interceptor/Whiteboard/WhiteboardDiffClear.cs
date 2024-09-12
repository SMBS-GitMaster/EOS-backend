using Newtonsoft.Json.Linq;
using NHibernate;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Interceptor.Whiteboard {
	public class WhiteboardDiffClear : IWhiteboardDiffInterceptor {


		public async Task ApplyAfter(ISession s, PermissionsUtility perms, long callerId, long orgId, Diff diff) {
			//noop
		}


		public async Task ApplyBefore(ISession s, PermissionsUtility perms, long callerId, long orgId, Diff diff) {
			var diffs = s.QueryOver<WhiteboardDiff>().Where(x => x.WhiteboardId == diff.Id && x.DeleteTime == null).List().ToList();
			var now = DateTime.UtcNow;
			foreach (var d in diffs) {
				d.DeleteTime = now;
				s.Update(d);
			}
		}


		public bool ShouldApplyInterceptor(ISession s, PermissionsUtility perms, long callerId, long orgId, Diff diff) {
			if (diff.ElementId != "project")
				return false;
			try {
				var parse = JObject.Parse(diff.Delta);
				if (!parse.ContainsKey("command"))
					return false;

				return ((string)parse["command"]) == "clearAll";
			} catch (Exception) {
				return false;
			}

		}

	}
}
