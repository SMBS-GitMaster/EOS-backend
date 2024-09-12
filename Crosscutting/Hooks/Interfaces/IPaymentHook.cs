using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Payments;
using RadialReview.Utilities.Calculators;
using System;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public class IInvoiceUpdates {
		public bool ForgivenChanged { get; set; }
		public bool PaidStatusChanged { get; set; }
		public bool ChargeRefunded { get; set; }
	}

	public class IPaymentHookChargeMetaData {
		public string TransactionId { get; set; }
		public long InvoiceId { get; set; }
		public decimal Subtotal { get; set; }
		public decimal TaxCollected { get; set; }
		public decimal AmountCollected { get; set; }

		public DateTime ChargeDate { get; set; }
		public ValidTaxLocation TaxLocation { get; set; }
		public bool Sandbox { get; set; }
		public string PaymentspringTransactionId { get; set; }
		public bool TaxExempt { get; set; }
		/// <summary>
		/// AmountPaid includes discounts. This field is purely informational.
		/// </summary>
		public decimal DiscountApplied { get; set; }

		[Obsolete("do not use")]
		public IPaymentHookChargeMetaData() {
		}

		public IPaymentHookChargeMetaData(long invoiceId, decimal subtotal, decimal taxCollected, decimal amountCollected, DateTime chargeDate, ValidTaxLocation taxLocation, bool sandbox, string paymentspringTransactionId, decimal discountApplied, bool taxExempt) {
			InvoiceId = invoiceId;
			Subtotal = subtotal;
			TaxCollected = taxCollected;
			AmountCollected = amountCollected;
			ChargeDate = chargeDate;
			TaxLocation = taxLocation;
			Sandbox = sandbox;
			PaymentspringTransactionId = paymentspringTransactionId;
			DiscountApplied = discountApplied;
			TaxExempt = taxExempt;
		}
	}

	public class IPaymentHookRefundMetaData {

		public IPaymentHookRefundMetaData(long organizationId, DateTime originalTransationTime, DateTime executionTime, long invoiceId, string paymentspringTransactionId, decimal subtotalRefundAmount, decimal taxRefundAmount, ValidTaxLocation validTaxLocation, bool useSandbox, bool isTaxExempt) {
			OrganizationId = organizationId;
			OriginalTransationTime = originalTransationTime;
			ExecutionTime = executionTime;
			InvoiceId = invoiceId;
			PaymentspringTransactionId = paymentspringTransactionId;
			SubtotalRefundAmount = subtotalRefundAmount;
			TaxRefundAmount = taxRefundAmount;
			TaxLocation = validTaxLocation;
			UseSandbox = useSandbox;
			IsTaxExempt = isTaxExempt;
		}

		public long OrganizationId { get; set; }
		public DateTime OriginalTransationTime { get; set; }
		public DateTime ExecutionTime { get; set; }
		public long InvoiceId { get; set; }
		public string PaymentspringTransactionId { get; set; }
		public decimal SubtotalRefundAmount { get; set; }
		public decimal TaxRefundAmount { get; set; }
		public ValidTaxLocation TaxLocation { get; internal set; }
		public bool UseSandbox { get; internal set; }
		public bool IsTaxExempt { get; internal set; }
	}

	public interface IPaymentHook : IHook {
		Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token, IPaymentHookChargeMetaData metaData);
		Task SuccessfulCharge(ISession s, PaymentSpringsToken token, decimal amount, IPaymentHookChargeMetaData metaData);

		Task CardExpiresSoon(ISession s, PaymentSpringsToken token);

		Task UpdateCard(ISession s, PaymentSpringsToken token);

		Task PaymentFailedUncaptured(ISession s, long orgId, DateTime executeTime, string errorMessage, bool firstAttempt);
		Task PaymentFailedCaptured(ISession s, long orgId, DateTime executeTime, PaymentException e, bool firstAttempt);

		Task RefundApplied(ISession s, IPaymentHookRefundMetaData metaData);

	}

	public interface IInvoiceHook : IHook {

		Task UpdateInvoice(ISession s, InvoiceModel invoice, IInvoiceUpdates updates);
		Task InvoiceCreated(ISession s, InvoiceModel invoice);
	}
}
