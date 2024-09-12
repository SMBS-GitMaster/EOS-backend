using System.Threading.Tasks;
using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Models;
using RadialReview.Utilities.Hooks;
using RadialReview.Accessors;
using RadialReview.Core.Repositories;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions {
	public class Subscription_L10_Tangent : ITangentHook {
		private readonly ITopicEventSender _eventSender;
		public Subscription_L10_Tangent(ITopicEventSender eventSender)
		{
			_eventSender = eventSender;
		}	

		public bool AbsorbErrors() {
			return false;
		}

		public bool CanRunRemotely() {
			return true;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task ShowTangent(ISession s, UserOrganizationModel caller, long recurrenceId) {
            var x = await s.GetAsync<RadialReview.Models.L10.L10Recurrence>(recurrenceId);
			var t = x.MeetingInstanceFromRecurrence(); 
            await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Updated(t.Id, t, null)).ConfigureAwait(false);      
		}
	}
}