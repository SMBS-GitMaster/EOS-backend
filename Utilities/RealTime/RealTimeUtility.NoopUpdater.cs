using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;

namespace RadialReview.Utilities.RealTime
{
    public partial class RealTimeUtility
    {
		public class NoopUpdater : BaseUpdater<NoopUpdater> {
			public NoopUpdater(RealTimeUtility rt) : base(rt) {}

			protected override IClientProxy ConstructProxy(IHubClients<IClientProxy> clients, UpdaterSettings settings, IReadOnlyList<string> excludedConnectionIds) {
				return clients.Client("noop-53c43c87c4504591abe7169f53dd34fe");
			}

			protected override IEnumerable<UpdaterSettings> GenerateUpdaterSettingsImpl() {
				return new UpdaterSettings[0];
			}
		}
	}
}
