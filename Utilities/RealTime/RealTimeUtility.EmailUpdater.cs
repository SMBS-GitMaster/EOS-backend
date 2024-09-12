using Microsoft.AspNetCore.SignalR;
using RadialReview.Hubs;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.RealTime {

	public partial class RealTimeUtility {
		public class UserEmailUpdater : BaseUpdater<UserEmailUpdater> {

			protected List<string> _emails = new List<string>();
			public UserEmailUpdater(IEnumerable<string> emails, RealTimeUtility rt) : base(rt) {
				_emails = emails.Where(x => !string.IsNullOrEmpty(x))
					.Select(x=>x.ToLower())
					.Distinct()
					.OrderBy(x=>x)
					.ToList();
			}

			protected override IEnumerable<UpdaterSettings> GenerateUpdaterSettingsImpl() {
				yield return UpdaterSettings.Create("email", _emails.Select(x=> RealTimeHub.Keys.Email(x)).ToArray());
			}

			protected override IClientProxy ConstructProxy(IHubClients<IClientProxy> clients, UpdaterSettings settings, IReadOnlyList<string> _) {
				//NOTICE: This cannot actually filter out excludedConnectionIds ..
				return clients.Groups(settings.GroupKeys.ToList().AsReadOnly());
			}
		}
	}
}
