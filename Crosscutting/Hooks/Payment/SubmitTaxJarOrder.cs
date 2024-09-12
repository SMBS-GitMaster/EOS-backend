using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models.Payments;
using RadialReview.Utilities.Calculators;
using RadialReview.Utilities.Hooks;
using System;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Payment {
	public class SubmitTaxJarOrder : IPaymentHook {


		public async Task SuccessfulCharge(ISession s, PaymentSpringsToken token, decimal amount, IPaymentHookChargeMetaData pr) {
			await TaxJarUtility.SubmitOrder(pr.InvoiceId, token, pr.Subtotal, pr.TaxCollected, pr.DiscountApplied, pr.ChargeDate, pr.TaxLocation, pr.Sandbox,pr.TaxExempt);
		}
		public async Task RefundApplied(ISession s, IPaymentHookRefundMetaData meta) {
			await TaxJarUtility.ApplyRefund(meta.InvoiceId, meta.SubtotalRefundAmount, meta.TaxRefundAmount, meta.OriginalTransationTime, meta.TaxLocation,meta.UseSandbox, meta.IsTaxExempt);
		}

		public async Task CardExpiresSoon(ISession s, PaymentSpringsToken token) {
			//noop
		}

		public async Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token, IPaymentHookChargeMetaData pr) {
			//noop
		}

		public async Task PaymentFailedCaptured(ISession s, long orgId, DateTime executeTime, PaymentException e, bool firstAttempt) {
			//noop
		}

		public async Task PaymentFailedUncaptured(ISession s, long orgId, DateTime executeTime, string errorMessage, bool firstAttempt) {
			//noop
		}

		public async Task UpdateCard(ISession s, PaymentSpringsToken token) {
			//noop
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Low;
		}

		public bool AbsorbErrors() {
			return true;
		}

		public bool CanRunRemotely() {
			return false;
		}

	}
}
