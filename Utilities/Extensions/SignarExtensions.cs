using Microsoft.AspNetCore.SignalR;

namespace RadialReview {
	public static class SignarExtensions {
		public static IClientProxy Group(this IHubCallerClients clients, string group, string excludeConnectionString) {
			return clients.GroupExcept(group, excludeConnectionString.AsList().AsReadOnly());
		}
	}
}
