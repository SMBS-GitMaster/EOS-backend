using Microsoft.AspNetCore.SignalR;
using RadialReview.Hubs;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.RealTime {

	public partial class RealTimeUtility {
		public class UserIdUpdater : BaseUpdater<UserIdUpdater> {

			protected List<long> _userIds = new List<long>();
			public UserIdUpdater(IEnumerable<long> userIds, RealTimeUtility rt) : base(rt) {
				_userIds = userIds.Where(x => x > 0).Distinct().OrderBy(x=>x).ToList();
			}

			protected override IEnumerable<UpdaterSettings> GenerateUpdaterSettingsImpl() {
				foreach (var uid in _userIds)
					yield return UpdaterSettings.Create("user", RealTimeHub.Keys.UserId(uid), uid);
			}

			protected override IClientProxy ConstructProxy(IHubClients<IClientProxy> clients, UpdaterSettings settings, IReadOnlyList<string> excludedConnectionIds) {
				return clients.GroupExcept(settings.GroupKeys.First(), excludedConnectionIds);
			}
		}
	}
}