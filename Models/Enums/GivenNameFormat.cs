namespace RadialReview.Models.Enums {
	public enum GivenNameFormat {
		FirstOnly,
		LastOnly,
		FirstAndLast,
	}

	public static class GivenNameFormatExtensions {
		public static string From(this GivenNameFormat format, string firstName, string lastName) {
			var builder = "";
			switch (format) {
				case GivenNameFormat.FirstOnly:
					builder = firstName;
					break;
				case GivenNameFormat.LastOnly:
					builder = lastName;
					break;
				case GivenNameFormat.FirstAndLast:
					builder = (firstName ?? "").Trim() + " " + (lastName ?? "").Trim();
					break;
				default:
					goto case GivenNameFormat.FirstAndLast;
			}

			return (builder).Trim();
		}
	}
}