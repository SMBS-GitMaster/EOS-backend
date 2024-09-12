using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Payments {

  public enum ItemizedType {
    Default = 0,
    BaseFee = 1,
    MeetingFee =2,
    PeopleToolsFee = 4,
    Credit = 8,
    Discount = 1073741824,
  }

	public class Itemized {
    public string BusinessUnit { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public decimal Price { get; set; }
		public decimal Quantity { get; set; }

    public ItemizedType Type { get; set; }

		public decimal Total() {
			return Price * Quantity;
		}

		public Itemized GenerateDiscount(string businessUnit) {
			return new Itemized() {
        BusinessUnit=businessUnit,
				Name = Name + " (Discounted)",
				Price = -1 * Price,
				Quantity = Quantity,
        Type = ItemizedType.Discount | Type
			};
		}

	}

	public static class ItemizedExtensions {
		public static decimal TotalCharge(this IEnumerable<Itemized> items) {
			return items.Sum(x => x.Total());
		}
	}
}