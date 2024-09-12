using Newtonsoft.Json;
using NHibernate;
using RadialReview.Crosscutting.Zapier;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {

	public class ZapierSubscriptionQuery {
		private List<ZapierSubscription> Subscriptions { get; set; }
		private DefaultDictionary<long, PermissionsUtility> Permissions { get; set; }
		private ISession Session { get; set; }


		public class SubscriptionResponse {
			public ZapierSubscription Subscription { get; internal set; }
			public object Response { get; internal set; }

			public string Serialized {
				get {
					return JsonConvert.SerializeObject(Response);
				}
			}
		}

		public ZapierSubscriptionQuery(ISession s, List<ZapierSubscription> subscriptions) {
			Subscriptions = subscriptions;
			Session = s;
			Permissions = new DefaultDictionary<long, PermissionsUtility>(callerId => PermissionsUtility.Create(Session, Session.Get<UserOrganizationModel>(callerId)));
		}

		public class PermissionTester {
			private PermissionsUtility Permissions { get; set; }
			public PermissionTester(PermissionsUtility permissions) {
				Permissions = permissions;
			}

			public T ShowIfPermitted<T>(Action<PermissionsUtility> subscriberPermission, T ifValid, T ifNotValid) {
				try {
					subscriberPermission(Permissions);
					return ifValid;
				} catch {
					return ifNotValid;
				}
			}
		}
		public List<SubscriptionResponse> GetSubscriptionResponses(long? itemId, long? accountableUserId, long? recurrenceId, Action<PermissionsUtility> subscriberPermissions, Func<PermissionTester, object> responseBuilder) {
			var arr = recurrenceId == null ? new long[] { } : new long[] { recurrenceId.Value };
			return GetSubscriptionResponses(itemId, accountableUserId, arr, subscriberPermissions, responseBuilder);
		}

		public List<SubscriptionResponse> GetSubscriptionResponses(long? itemId, long? accountableUserId, long[] recurrenceIds, Action<PermissionsUtility> subscriberPermissions, Func<PermissionTester, object> responseBuilder) {
			recurrenceIds = recurrenceIds ?? new long[] { };
			return Subscriptions.Where(subscription => {
				try {
					if (subscription.FilterOnAccountableUserId != null && subscription.FilterOnAccountableUserId != accountableUserId) {
						return false;
					}
					if (subscription.FilterOnItemId != null && subscription.FilterOnItemId != itemId) {
						return false;
					}
					if (subscription.FilterOnL10RecurrenceId != null && recurrenceIds.All(x => x != subscription.FilterOnL10RecurrenceId)) {
						return false;
					}

					var subscriberPerms = Permissions[subscription.SubscriberId];
					subscriberPermissions(subscriberPerms);
					return true;
				} catch {
					return false;
				}
			}).Select(x => {
				var subscriberPerms = Permissions[x.SubscriberId];
				var permTester = new PermissionTester(subscriberPerms);
				return new SubscriptionResponse {
					Subscription = x,
					Response = responseBuilder(permTester)
				};
			}).ToList();
		}




	}

	public class ZapierAccessor {

		public static bool IsZapierEnabled_Unsafe(ISession s, long orgId) {
			if (Config.IsZapierDisabled()) {
				return false;
			}
			if (s.Get<OrganizationModel>(orgId).Settings.EnableZapier) {
				if (s.GetSettingOrDefault(Variable.Names.ZAPIER_ENABLED, () => true)) {
					return true;
				}
			}
			return false;
		}

		public static async Task<ZapierSubscription> SaveZapierSubscription(UserOrganizationModel caller, long subscriberUserId, long organizationId, string zapierId, string zapierTargetUrl, ZapierEvents @event, long? filter_on_item_id, long? filter_on_accountableUserId, long? filter_on_L10Recurrence_id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(subscriberUserId);
					perms.ViewOrganization(organizationId);

					if (!IsZapierEnabled_Unsafe(s, organizationId)) {
						throw new PermissionsException("Zapier is not enabled for this organization. Contact Support.");
					}


					if (zapierTargetUrl == null) {
						throw new ArgumentNullException(nameof(zapierTargetUrl));
					}

					if (zapierId == "0") {
						throw new ArgumentOutOfRangeException(nameof(zapierId));
					}

					if (@event == ZapierEvents.invalid) {
						throw new ArgumentOutOfRangeException("Unexpected Zapier Event Type");
					}
					if (filter_on_accountableUserId != null) {
						perms.ViewUserOrganization(filter_on_accountableUserId.Value, true);
					}

					if (filter_on_L10Recurrence_id != null) {
						perms.ViewL10Recurrence(filter_on_L10Recurrence_id.Value);
					}

					if (filter_on_item_id != null) {
						EnsureItemFilter(perms, @event, filter_on_item_id.Value);
					}
					var sub = new ZapierSubscription() {
						SubscriberId = subscriberUserId,
						Event = @event,
						FilterOnAccountableUserId = filter_on_accountableUserId,
						FilterOnL10RecurrenceId = filter_on_L10Recurrence_id,
						FilterOnItemId = filter_on_item_id,
						OrgId = organizationId,
						TargetUrl = zapierTargetUrl,
						ZapierId = zapierId
					};
					s.Save(sub);
					tx.Commit();
					s.Flush();
					return sub;
				}
			}
		}

		private static void EnsureItemFilter(PermissionsUtility perms, ZapierEvents @event, long itemId) {
			switch (@event) {

				case ZapierEvents.update_todo:
				case ZapierEvents.all_todo:
					perms.ViewTodo(itemId);
					break;
				case ZapierEvents.update_issue:
				case ZapierEvents.all_issue:
					perms.ViewIssueRecurrence(itemId);
					break;
				case ZapierEvents.update_rock:
				case ZapierEvents.attach_rock:
				case ZapierEvents.all_rock:
					perms.ViewRock(itemId);
					break;
				case ZapierEvents.update_headline:
				case ZapierEvents.all_headline:
					perms.ViewHeadline(itemId);
					break;
				case ZapierEvents.update_measurable:
				case ZapierEvents.all_measurable:
				case ZapierEvents.attach_measurable:
					perms.ViewMeasurable(itemId);
					break;
				case ZapierEvents.update_score:
					perms.ViewMeasurable(itemId);
					break;
				case ZapierEvents.start_process:
				case ZapierEvents.complete_process:
					perms.ViewProcess(itemId);
					break;
				case ZapierEvents.new_todo:
				case ZapierEvents.new_issue:
				case ZapierEvents.new_rock:
				case ZapierEvents.new_headline:
				case ZapierEvents.new_measurable:
				case ZapierEvents.new_process:
					throw new Exception("Cannot subscribe to an uncreated item");
				case ZapierEvents.complete_process_step:
					perms.ViewProcessStep(itemId);
					break;
				default:
					throw new PermissionsException("Unhandled Permission: EnsureItemFilter");
			}
		}

		public static async Task DeleteZapierSubscriptions(UserOrganizationModel caller, long subscriberId, string zapierId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.EditZapierSubscription(zapierId);
					perms.Self(subscriberId);


					var zapier = s.QueryOver<ZapierSubscription>()
										.Where(x => x.DeleteTime == null && x.ZapierId == zapierId && x.SubscriberId == subscriberId)
										.List().ToList();

					foreach (var zapierSubscription in zapier) {

						zapierSubscription.DeleteTime = DateTime.UtcNow;
						s.Update(zapierSubscription);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}



		public static ZapierSubscriptionQuery GetZapierSubscriptions_unsafe(ISession s, long forOrganizationId, ZapierEvents event_type) {
			ZapierEvents both_event = event_type;
			if (event_type == ZapierEvents.new_todo || event_type == ZapierEvents.update_todo)
				both_event = ZapierEvents.all_todo;
			else if (event_type == ZapierEvents.new_issue || event_type == ZapierEvents.update_issue)
				both_event = ZapierEvents.all_issue;
			else if (event_type == ZapierEvents.new_rock || event_type == ZapierEvents.update_rock || event_type == ZapierEvents.attach_rock)
				both_event = ZapierEvents.all_rock;
			else if (event_type == ZapierEvents.new_headline || event_type == ZapierEvents.update_headline)
				both_event = ZapierEvents.all_headline;
			else if (event_type == ZapierEvents.new_measurable || event_type == ZapierEvents.update_measurable || event_type == ZapierEvents.attach_measurable)
				both_event = ZapierEvents.all_measurable;
			else if (event_type == ZapierEvents.start_process || event_type == ZapierEvents.complete_process || event_type == ZapierEvents.new_process || event_type == ZapierEvents.delete_process)
				both_event = ZapierEvents.all_process;


			var subs = s.QueryOver<ZapierSubscription>()
				.Where(x => x.DeleteTime == null && x.OrgId == forOrganizationId && (x.Event == event_type || x.Event == both_event))
				.List<ZapierSubscription>().ToList();

			return new ZapierSubscriptionQuery(s, subs);

		}


	}
}
