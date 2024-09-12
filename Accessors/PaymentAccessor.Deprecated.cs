using Hangfire;
using Newtonsoft.Json;
using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.Hangfire;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Payments;
using RadialReview.Models.Tasks;
using RadialReview.Models.UserModels;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.Calculators;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Extensions;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.NHibernate;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.Utilities.PaymentSpringUtil;
using RadialReview.Nhibernate;
using RadialReview.Hangfire.Activator;
using Microsoft.Extensions.Logging;

namespace RadialReview.Accessors {
	public partial class PaymentAccessor : BaseAccessor {
		public class HangfireChargeResult {
			public PaymentResult PaymentResult { get; set; }
			public bool WasCharged { get; set; }
			public string Message { get; set; }
			public bool HasError { get; set; }
			public bool WasFallthrough { get; set; }
			public bool WasPaymentException { get; set; }

			public HangfireChargeResult(PaymentResult paymentResult, bool wasCharged, bool wasFallthrough, bool hasBreakingError, bool wasPaymentException, string message) {
				WasCharged = wasCharged;
				PaymentResult = paymentResult;
				Message = message;
				HasError = hasBreakingError;
				WasFallthrough = wasFallthrough;
				WasPaymentException = wasPaymentException;
			}
		}

		public partial class Unsafe : IChargeViaHangfire {
			public class ChargeResult {
				public InvoiceModel Invoice { get; set; }
				public PaymentResult Result { get; set; }
			}

			//[Obsolete("Unsafe")]
			[Obsolete("out dated")]
			public static async Task<ChargeResult> ChargeOrganization_Unsafe(ISession s, OrganizationModel org, PaymentPlanModel plan, string guid, DateTime executeTime, bool forceUseTest, bool firstAttempt) {
				try {
					var o = new ChargeResult();
					o.Invoice = await GenerateInvoiceModelsForAllJointAccounts(s, org, plan, guid, executeTime, forceUseTest);
					o.Result = await ExecuteInvoice(s, o.Invoice, forceUseTest);
					return o;
				} catch (PaymentException e) {
					await HooksRegistry.Each<IPaymentHook>((ses, x) => x.PaymentFailedCaptured(ses, org.Id, executeTime, e, firstAttempt));
					log.Error("PaymentAccessor.cs: line 918", e);
					throw;
				} catch (Exception e) {
					if (!(e is FallthroughException)) {
						await HooksRegistry.Each<IPaymentHook>((ses, x) => x.PaymentFailedUncaptured(ses, org.Id, executeTime, e.Message, firstAttempt));
					}
					log.Error("PaymentAccessor.cs: line 924", e);
					throw;
				}
			}


			/*
			 * There are no tests to ensure that this task is unstarted. Tests were performed in the EnqueueChargeOrganizationFromTask method above
			 */
			[Obsolete("DEPRECATED... Must call with Enqueue. Cannot be run again through ScheduledTask (task is marked complete), cannot be called inside a session. Calling a second time will charge a second time.", true)]
			[Queue(HangfireQueues.Immediate.CHARGE_ACCOUNT_VIA_HANGFIRE)]
			[AutomaticRetry(Attempts = 0)]
			public async Task<HangfireChargeResult> ChargeViaHangfire(long organizationId, long unverified_taskId, bool forceUseTest, bool sendReceipt, DateTime executeTime) {
				PaymentResult result = null;
				ChargeResult chargeResult = null;
				try {
					log.Info("ChargingOrganization(" + organizationId + ")");
					var guid = "" + Guid.NewGuid();
					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							var org = s.Get<OrganizationModel>(organizationId);
							var plan = org.PaymentPlan;
							try {
								chargeResult = await ChargeOrganization_Unsafe(s, org, plan, guid, executeTime, forceUseTest, true);
								result = chargeResult.Result;
							} finally {
								tx.Commit();
								s.Flush();
							}
						}
					}
					if (sendReceipt) {
						var batchId = BatchJob.StartNew(batch => { }, "Send Receipt");
						await ScheduleReceipt(batchId, result, chargeResult.Invoice.Id);
					}
					log.Info("ChargedOrganization(" + organizationId + ")");
					return new HangfireChargeResult(result, true, false, false, false, "charged");
				} catch (PaymentException capturedPaymentException) {
					await RecordCapturedPaymentException(capturedPaymentException, unverified_taskId);
					//Saved exception.. stop execution
					throw;
					//return new HangfireChargeResult(null, false, false, true, true, "" + capturedPaymentException.Type);
				} catch (FallthroughException e) {
					log.Error("FallthroughCaptured", e);
					//It's a fallthrough, stop execution
					return new HangfireChargeResult(null, false, true, false, true, e.NotNull(x => x.Message) ?? "Exception was null");
				} catch (Exception capturedException) {
					await RecordUnknownPaymentException(capturedException, organizationId, unverified_taskId);
					//Email send.. stop execution.
					throw;
					//return new HangfireChargeResult(null, false, false, true, false, capturedException.NotNull(x => x.Message) ?? "-no message-");
				}
			}

		}
	}
}
