using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.RealTime {
	public partial class RealTimeUtility {
		public class GroupUpdater : BaseUpdater<GroupUpdater> {
			protected List<string> GroupNames;

			public GroupUpdater(IEnumerable<string> groupNames, RealTimeUtility rt) : base(rt) {
				if (groupNames == null)
					throw new ArgumentNullException(nameof(groupNames));
				GroupNames = groupNames.Distinct().OrderBy(x => x).ToList();
			}

			protected override IEnumerable<UpdaterSettings> GenerateUpdaterSettingsImpl() {
				foreach (var group in GroupNames) {
					yield return UpdaterSettings.Create("group", group);
				}
			}

			protected override IClientProxy ConstructProxy(IHubClients<IClientProxy> clients, UpdaterSettings settings, IReadOnlyList<string> excludedConnectionIds) {
				return clients.GroupExcept(settings.GroupKeys.Single(), excludedConnectionIds);
			}
		}
	}
}
