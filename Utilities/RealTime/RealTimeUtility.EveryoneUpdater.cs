using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;

namespace RadialReview.Utilities.RealTime {
	public partial class RealTimeUtility {
		public class EveryoneUpdater : BaseUpdater<EveryoneUpdater> {
			public EveryoneUpdater(RealTimeUtility rt) : base(rt) {

			}
			protected override IClientProxy ConstructProxy(IHubClients<IClientProxy> clients, UpdaterSettings settings, IReadOnlyList<string> excludedConnectionIds) {
				return clients.AllExcept(excludedConnectionIds);
			}

			protected override IEnumerable<UpdaterSettings> GenerateUpdaterSettingsImpl() {
				yield return UpdaterSettings.Create("everyone", "everyone");
			}
		}

	}
}
