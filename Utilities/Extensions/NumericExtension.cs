namespace RadialReview.Utilities.Extensions {
	public static class NumericExtension {

		public static bool IsBetween(this decimal value, decimal minimum, decimal maximum) {
			return value >= minimum && value <= maximum;
		}
	}
}