using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.Attributes {
	public class EmailAddressOrEmptyAttribute : DataTypeAttribute {
		public EmailAddressOrEmptyAttribute() : base(DataType.EmailAddress) { }

		public override bool IsValid(object value) {
			if (value == null) {
				return true;
			}
			string input = value as string;
			return (input != null) && (string.IsNullOrEmpty(input) || (new EmailAddressAttribute()).IsValid(input));
		}
	}
}