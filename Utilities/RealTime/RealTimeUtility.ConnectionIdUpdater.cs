using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.RealTime {
	public partial class RealTimeUtility {
		public class ConnectionIdUpdater : BaseUpdater<ConnectionIdUpdater> {
			protected string[] ClientIds;
			public ConnectionIdUpdater(IEnumerable<string> clientIds, RealTimeUtility rt) : base(rt) {
				if (clientIds == null)
					throw new ArgumentNullException(nameof(clientIds));
				ClientIds = clientIds.Distinct().OrderBy(x=>x).ToArray();
			}
			protected override IEnumerable<UpdaterSettings> GenerateUpdaterSettingsImpl() {
				yield return UpdaterSettings.Create("connectionId", ClientIds);
			}

			protected override IClientProxy ConstructProxy(IHubClients<IClientProxy> clients, UpdaterSettings settings, IReadOnlyList<string> excludedConnectionIds) {
				var keysLessExcluded = settings.GroupKeys.Where(x => !excludedConnectionIds.Any(y => y == x)).ToList();
				return clients.Clients(keysLessExcluded.AsReadOnly());
			}						
		}
	}
}
