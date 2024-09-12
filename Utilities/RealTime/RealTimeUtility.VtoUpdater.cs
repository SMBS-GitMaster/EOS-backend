using Microsoft.AspNetCore.SignalR;
using RadialReview.Hubs;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.RealTime {
	public partial class RealTimeUtility {
		public class VtoUpdater : BaseUpdater<VtoUpdater> {

			protected List<long> _vtoIds = new List<long>();
			public VtoUpdater(IEnumerable<long> vtos, RealTimeUtility rt)  : base(rt){
				_vtoIds = vtos.Distinct().ToList();
			}		

			protected override IEnumerable<UpdaterSettings> GenerateUpdaterSettingsImpl() {
				foreach (var vid in _vtoIds) {
					yield return UpdaterSettings.Create("vto", RealTimeHub.Keys.GenerateVtoGroupId(vid), vid);
				}
			}

			protected override IClientProxy ConstructProxy(IHubClients<IClientProxy> clients, UpdaterSettings settings, IReadOnlyList<string> excludedConnectionIds) {
				return clients.GroupExcept(settings.GroupKeys.Single(), excludedConnectionIds);
			}			
		}
	}
}