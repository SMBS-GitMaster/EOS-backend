using RadialReview.Utilities.Hooks;
using System;
using NHibernate;
using RadialReview.Models.Payments;
using System.Threading.Tasks;
using RadialReview.Exceptions;

namespace RadialReview.Crosscutting.Hooks.CrossCutting.Payment {
	public class FirstPaymentEmail : IPaymentHook {
		public bool CanRunRemotely() {
			return false;
		}
		public bool AbsorbErrors() {
			return false;
		}

		public async Task CardExpiresSoon(ISession s, PaymentSpringsToken token) {
			//noop
		}

		public async Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token, IPaymentHookChargeMetaData metaData) {
			//noop
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Low;
		}

		public async Task PaymentFailedCaptured(ISession s, long orgId, DateTime executeTime, PaymentException e, bool firstAttempt) {
			//noop
		}

		public async Task PaymentFailedUncaptured(ISession s, long orgId, DateTime executeTime, string errorMessage, bool firstAttempt) {
			//noop
		}

		public async Task SuccessfulCharge(ISession s, PaymentSpringsToken token, decimal amount, IPaymentHookChargeMetaData metaData) {
			//noop
		}

		public async Task UpdateCard(ISession s, PaymentSpringsToken token) {
			//noop
		}
		public async Task RefundApplied(ISession s, IPaymentHookRefundMetaData metaData) {
			//noop
		}
	}
}
