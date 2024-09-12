using System.ComponentModel.DataAnnotations;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Models.Payments;
using RadialReview.Utilities.Calculators;

namespace RadialReview.Models {



	public class InvoiceModel : ILongIdentifiable, IHistorical {

		public static readonly string STATUS_UNSTARTED = "unstarted";
		public static readonly string STATUS_IN_PROGRESS = "in-progress";
		public static readonly string STATUS_COMPLETE = "complete";
		public static readonly string STATUS_FAILED = "failed";


		public virtual long Id { get; protected set; }
		[Display(Name = "Sent"), DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
		public virtual DateTime InvoiceSentDate { get; set; }
		[Display(Name = "Due"), DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
		public virtual DateTime InvoiceDueDate { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual IList<InvoiceItemModel> InvoiceItems { get; set; }

		public virtual string BatchGuid { get; set; }
		public virtual string ChargeStatus { get; set; }

		public virtual IList<InvoiceUserItemModel> _InvoiceUserItems { get; set; }

		public virtual long? ForgivenBy { get; set; }
		public virtual long? ManuallyMarkedPaidBy { get; set; }

		[Display(Name = "Date Paid"), DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
		public virtual DateTime? PaidTime { get; set; }

		[Display(Name = "Transaction Id")]
		public virtual String TransactionId { get; set; }

		public virtual DateTime ServiceStart { get; set; }
		[Display(Name = "Service Through"), DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
		public virtual DateTime ServiceEnd { get; set; }

		[Display(Name = "Amount Due"), DisplayFormat(DataFormatString = "${0:f2}", ApplyFormatInEditMode = false)]
		public virtual decimal AmountDue { get; set; }

		public virtual String EmailAddress { get; set; }

		public virtual decimal? TaxDue { get; set; }
		public virtual decimal? Subtotal { get; set; }
		public virtual decimal? TaxRate { get; set; }
		public virtual decimal? SubtotalRefunded { get; set; }
		public virtual decimal? TaxRefunded { get; set; }

		public virtual bool IsTest { get; set; }
		
		/// <summary>
		/// Cache for the tax location. Not saved.
		/// </summary>
		public virtual ValidTaxLocation _TaxLocation { get; set; }
		public virtual bool TaxExempt { get; set; }


		public virtual bool WasAutomaticallyPaid() {
			return ManuallyMarkedPaidBy == null && PaidTime != null;
		}

		public virtual bool WasPaid() {
			return !AnythingDue() && AmountDue > 0;
		}

		public virtual bool AnythingDue() {
			return !(PaidTime != null || AmountDue <= 0 || ForgivenBy != null);
		}



		public InvoiceModel() {
			InvoiceItems = new List<InvoiceItemModel>();
			CreateTime = DateTime.UtcNow;
			ChargeStatus = STATUS_UNSTARTED;
		}

		public virtual decimal? GetDiscount() {
			if (InvoiceItems == null)
				return null;
			return InvoiceItems.Where(x => x.AmountDue <= 0).Sum(x => x.AmountDue);
		}

		public virtual decimal GetSubtotal() {
			return Subtotal ?? (AmountDue - (TaxDue ?? 0));
		}
	}

	public class InvoiceModelMap : ClassMap<InvoiceModel> {
		public InvoiceModelMap() {
			Id(x => x.Id);
			Map(x => x.InvoiceSentDate);
			Map(x => x.InvoiceDueDate);
			Map(x => x.PaidTime);
			Map(x => x.CreateTime);
			Map(x => x.DeleteTime);
			Map(x => x.TransactionId);

			Map(x => x.ForgivenBy);
			Map(x => x.ManuallyMarkedPaidBy);

			Map(x => x.AmountDue);
			Map(x => x.TaxDue);
			Map(x => x.TaxRate);
			Map(x => x.Subtotal);
			Map(x => x.IsTest);
			Map(x => x.TaxExempt);
			Map(x => x.SubtotalRefunded);
			Map(x => x.TaxRefunded);

			Map(x => x.BatchGuid).Index("invoice_batchguid_idx");
			Map(x => x.ChargeStatus);

			Map(x => x.EmailAddress);

			Map(x => x.ServiceStart);
			Map(x => x.ServiceEnd);

			References(x => x.Organization);
			HasMany(x => x.InvoiceItems)
				.Table("InvoiceItems")
				.Cascade.SaveUpdate();
		}

	}
}
