using Hangfire;
using log4net;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.Hangfire;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Payments;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using static RadialReview.Utilities.Config;

namespace RadialReview.Crosscutting.Hooks.Internal {
	public class InternalZapierHooks : ILegalHooks, ICreateUserOrganizationHook, IUpdateUserModelHook, IOrganizationHook, IPaymentHook {
		#region Helper
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public bool AbsorbErrors() {
			return true;
		}

		public bool CanRunRemotely() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Lowest;
		}
		#endregion


		#region Users
		public class ZapierUser {
			public string type { get; set; }
			public string action { get; set; }
			public string first_name { get; set; }
			public string last_name { get; set; }
			public string email { get; set; }
			public string user_guid { get; set; }
			public DateTime create_time { get; set; }
		}

		public class ZapierUserOrg {
			public string type { get; set; }
			public string action { get; set; }
			public string first_name { get; set; }
			public string last_name { get; set; }
			public string email { get; set; }
			public string user_guid { get; set; }
			public string organization { get; set; }
			public long user_id { get; set; }
			public long org_id { get; set; }
			public bool is_account_admin { get; set; }
			public bool is_manager { get; set; }
			public DateTime create_time { get; set; }
			public DateTime? attach_time { get; set; }
			public DateTime? delete_time { get; set; }
			public DateTime? detach_time { get; set; }
			public bool is_placeholder { get; set; }
		}

		public async Task OnUserOrganizationAttach(ISession s, UserOrganizationModel user, OnUserOrganizationAttachData data) {
			var u = ConstructUser("attach", user);
			Scheduler.Enqueue(() => SendToZapier(u, InternalZapierEndpointType.UserOrganizationAttach));
		}

		public async Task CreateUserOrganization(ISession s, UserOrganizationModel user, CreateUserOrganizationData data) {
			Scheduler.Enqueue(() => SendToZapier(
				ConstructUser("create", user),
				InternalZapierEndpointType.UserOrganizationCreate
			));
		}

		public async Task UpdateUserModel(ISession s, UserModel user) {
			Scheduler.Enqueue(() => SendToZapier(
				ConstructUser("update", user),
				InternalZapierEndpointType.UserUpdate
			));
		}

		public async Task OnUserRegister(ISession s, UserModel user, OnUserRegisterData data) {
			Scheduler.Enqueue(() => SendToZapier(
				ConstructUser("register", user),
				InternalZapierEndpointType.UserUpdate
			));
		}

		private ZapierUserOrg ConstructUser(string action, UserOrganizationModel user) {
			return new ZapierUserOrg {
				type = "user",
				action = action,
				first_name = user.GetFirstName(),
				last_name = user.GetLastName(),
				email = user.GetEmail(),
				organization = user.Organization.NotNull(x => x.GetName()),
				user_id = user.Id,
				user_guid = user.User.NotNull(x => x.Id),
				org_id = user.Organization.NotNull(x => x.Id),
				is_account_admin = user.ManagingOrganization,
				is_manager = user.ManagerAtOrganization,
				attach_time = user.AttachTime,
				create_time = user.CreateTime,
				delete_time = user.DeleteTime,
				detach_time = user.DetachTime,
				is_placeholder = user.IsPlaceholder,
			};
		}

		private ZapierUser ConstructUser(string action, UserModel user) {
			return new ZapierUser {
				type = "user",
				action = action,
				first_name = user.FirstName,
				last_name = user.LastName,
				email = user.Email,
				user_guid = user.Id,
				create_time = user.CreateTime,
			};
		}

		#endregion

		#region Organization

		public class ZapierOrg {
			public string type { get; set; }
			public string action { get; set; }
			public string name { get; set; }
			public long org_id { get; set; }
			public AccountType account_type { get; set; }
			public DateTime create_time { get; set; }
			public DateTime? delete_time { get; set; }
			public bool eval_enabled { get; set; }
			public bool people_enabled { get; set; }
			public bool l10_enabled { get; set; }
			public bool core_process_enabled { get; set; }
		}

		public async Task CreateOrganization(ISession s, UserOrganizationModel creator, OrganizationModel organization, OrgCreationData createData, IOrganizationHookCreate meta) {
			Scheduler.Enqueue(() => SendToZapier(ConstructOrg("create", organization), InternalZapierEndpointType.OrganizationCreate));

		}

		public async Task UpdateOrganization(ISession s, long organizationId, IOrganizationHookUpdates updates, UserOrganizationModel user) {
			var org = s.Get<OrganizationModel>(organizationId);
			Scheduler.Enqueue(() => SendToZapier(ConstructOrg("update", org), InternalZapierEndpointType.OrganizationUpdate));
		}

		private ZapierOrg ConstructOrg(string action, OrganizationModel org) {
			return new ZapierOrg {
				type = "org",
				action = action,
				org_id = org.Id,
				account_type = org.AccountType,
				create_time = org.CreationTime,
				delete_time = org.DeleteTime,
				eval_enabled = org.Settings.EnableReview,
				people_enabled = org.Settings.EnablePeople,
				l10_enabled = org.Settings.EnableL10,
				core_process_enabled = org.Settings.EnableCoreProcess,

				name = org.GetName(),
			};
		}
		#endregion

		#region Payment

		public class ZapierCard {
			public string type { get; set; }
			public string action { get; set; }
			public long org_id { get; set; }
			public int payment_card_month_expire { get; set; }
			public int payment_card_year_expire { get; set; }
			public string payment_owner { get; set; }
			public string payment_owner_email { get; set; }
			public string payment_owner_phone { get; set; }
			public string payment_address_1 { get; set; }
			public string payment_address_2 { get; set; }
			public string payment_city { get; set; }
			public string payment_state { get; set; }
			public string payment_zip { get; set; }
			public string payment_website { get; set; }
			public string payment_country { get; set; }
			public string payment_type { get; set; }

			public static ZapierCard Create(string action, PaymentSpringsToken token) {
				return new ZapierCard {
					type = "org",
					action = action,
					org_id = token.OrganizationId,
					payment_address_1 = token.Address_1,
					payment_address_2 = token.Address_2,
					payment_card_month_expire = token.MonthExpire,
					payment_card_year_expire = token.YearExpire,
					payment_city = token.City,
					payment_country = token.Country,
					payment_owner = token.CardOwner,
					payment_owner_email = token.ReceiptEmail,
					payment_owner_phone = token.Phone,
					payment_state = token.State,
					payment_type = "" + token.TokenType,
					payment_website = token.Website,
					payment_zip = token.Zip,
				};
			}
		}
		public class ZapierCharge {
			public enum Status {
				Success,
				Failed,
				UnhandledError
			}

			public static ZapierCharge Create(long orgId,string transactionId, decimal? amount, DateTime date, Status status, string message) {
				return new ZapierCharge {
					type = "payment",
					action = "charge",
					org_id = orgId,
					status = status,
					amount = amount,
					date = date,
					message = message,
					transaction_id = transactionId
				};
			}

			public static ZapierCharge Refund(long orgId,string transactionId, decimal? refundedSubtotal, decimal? refundedTax, decimal? totalRefund, DateTime date, Status status, string message) {
				return new ZapierCharge {
					type = "payment",
					action = "refund",
					org_id = orgId,
					status = status,
					amount = totalRefund,
					refunded_subtotal = refundedSubtotal,
					refunded_tax = refundedTax,
					date = date,
					message = message,
					transaction_id = transactionId
				};
			}

			public string type { get; set; }
			public string action { get; set; }
			public long org_id { get; set; }
			public decimal? amount { get; set; }
			public decimal? refunded_subtotal { get; set; }
			public decimal? refunded_tax { get; set; }
			public DateTime date { get; set; }
			public string message { get; set; }
			public string transaction_id { get; set; }
			public Status status { get; set; }
		}

		public async Task UpdateCard(ISession s, PaymentSpringsToken token) {
			Scheduler.Enqueue(() => SendToZapier(ZapierCard.Create("update", token), InternalZapierEndpointType.PaymentUpdate));
		}

		public async Task SuccessfulCharge(ISession s, PaymentSpringsToken token, decimal amount, IPaymentHookChargeMetaData metaData) {
			Scheduler.Enqueue(() =>
			SendToZapier(
				ZapierCharge.Create(token.OrganizationId, metaData.TransactionId, amount, DateTime.UtcNow, ZapierCharge.Status.Success, "ok"),
				InternalZapierEndpointType.AccountCharged
			));
		}

		public async Task PaymentFailedUncaptured(ISession s, long orgId, DateTime executeTime, string errorMessage, bool firstAttempt) {
			Scheduler.Enqueue(() => SendToZapier(
				ZapierCharge.Create(orgId,null, 0m, DateTime.UtcNow, ZapierCharge.Status.UnhandledError, errorMessage),
				InternalZapierEndpointType.AccountCharged
			));
		}

		public async Task PaymentFailedCaptured(ISession s, long orgId, DateTime executeTime, PaymentException e, bool firstAttempt) {
			Scheduler.Enqueue(() => SendToZapier(
				ZapierCharge.Create(orgId,null, 0m, DateTime.UtcNow, ZapierCharge.Status.Failed, e.Message),
				InternalZapierEndpointType.AccountCharged
			));
		}

		public async Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token, IPaymentHookChargeMetaData metaData) {
			//noop
		}

		public async Task CardExpiresSoon(ISession s, PaymentSpringsToken token) {
			//noop
		}

		public async Task RefundApplied(ISession s, IPaymentHookRefundMetaData metaData) {
			Scheduler.Enqueue(() =>
				SendToZapier(
					ZapierCharge.Refund(metaData.OrganizationId, metaData.PaymentspringTransactionId, metaData.SubtotalRefundAmount, metaData.TaxRefundAmount, metaData.SubtotalRefundAmount + metaData.TaxRefundAmount, metaData.ExecutionTime, ZapierCharge.Status.Success, "ok"),
					InternalZapierEndpointType.AccountCharged
				)
			);
		}

		#endregion

		#region Utility

		public class InternalMetaEndpoint {
			public string Url { get; set; }
			public List<string> Urls { get; set; }

			public bool UrlEnabled { get; set; }
			public bool ZapierEnabled { get; set; }

			public IEnumerable<string> GetUrls() {
				if (!UrlEnabled)
					yield break;
				if (!string.IsNullOrWhiteSpace(Url))
					yield return Url;
				if (Urls != null) {
					foreach (var u in Urls) {
						if (!string.IsNullOrWhiteSpace(u))
							yield return u;
					}
				}
				yield break;
			}

			public InternalMetaEndpoint() {
				ZapierEnabled = true;
				UrlEnabled = false;
			}
		}

		public class SendToZapierResult {
			public bool UrlExecuted { get; set; }
			public bool ZapierExecuted { get; set; }
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_SEND_INTERNAL)]
		[AutomaticRetry(Attempts = 0)]
		public async Task<SendToZapierResult> SendToZapier(object obj, InternalZapierEndpointType type) {
			InternalMetaEndpoint internalMetaEndpoint = null;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					internalMetaEndpoint = s.GetSettingOrDefault(Variable.Names.INTERNAL_META_ENDPOINT, () => new InternalMetaEndpoint() {
						ZapierEnabled = true,
						UrlEnabled = false
					});
					tx.Commit();
					s.Flush();
				}
			}

			var urlExecuted = false;
			var zapierExecuted = false;


			foreach (var u in internalMetaEndpoint.GetUrls()) {
				using (var client = new HttpClient()) {
					try {
						await client.PostAsJsonAsync(u, new {
							name = type.Type,
							data = obj
						});
						urlExecuted = true;
					} catch (Exception e) {
						log.Error("MetaEndpoint: Internal Zapier Error", e);
					}
				}
			}

			if ((internalMetaEndpoint.ZapierEnabled && ShouldRun(type))) {
				using (var client = new HttpClient()) {
					try {
						await client.PostAsJsonAsync(Endpoint(type), obj);
						zapierExecuted = true;
					} catch (Exception e) {
						log.Error("Internal Zapier Error", e);
						throw;
					}
				}
			}

			return new SendToZapierResult {
				UrlExecuted = urlExecuted,
				ZapierExecuted = zapierExecuted,
			};

		}
		public string Endpoint(InternalZapierEndpointType type) {
			return Config.GetInternalZapierEndpoint(type);
		}

		public bool ShouldRun(InternalZapierEndpointType type) {
			return !String.IsNullOrWhiteSpace(Endpoint(type));
		}

		#endregion

		#region Legal
		public async Task SubmitTerms(ISession s, string userId, string termId, string kind, string termsSha1, bool accept) {
			var terms = s.Get<TermsAcceptanceModel>(termId);
			Scheduler.Enqueue(() => SendToZapier(terms, InternalZapierEndpointType.TermsSubmitted));
		}
		#endregion

	}
}
