using Microsoft.AspNetCore.SignalR;
using RadialReview.Hubs;
using RadialReview.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.RealTime {


	public partial class RealTimeUtility {
		public class OrganizationUpdater : BaseUpdater<OrganizationUpdater> {

			protected long _OrganizationId { get; set; }

			public OrganizationUpdater(long orgId, RealTimeUtility rt) : base(rt) {
				_OrganizationId = orgId;
			}
			protected override IEnumerable<UpdaterSettings> GenerateUpdaterSettingsImpl() {
				yield return UpdaterSettings.Create("org", RealTimeHub.Keys.OrganizationId(_OrganizationId), _OrganizationId);
			}

			protected override IClientProxy ConstructProxy(IHubClients<IClientProxy> clients, UpdaterSettings group, IReadOnlyList<string> excludedConnectionIds) {
				return clients.GroupExcept(group.GroupKeys.Single(), excludedConnectionIds);
			}

			[Obsolete("not working", true)]
			public OrganizationUpdater NotificationStatus(long notificationId, bool seen, string username) {
				

				return this;
			}

			[Obsolete("not working",true)]
			public OrganizationUpdater Notification(Notification notification, IEnumerable<string> usernames) {
				return this;
			}

		}

		
	}
}
