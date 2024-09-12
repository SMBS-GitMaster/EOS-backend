using RadialReview.Models.Charts;
using System;
using System.Text.RegularExpressions;

namespace RadialReview {
	public static class ChartExtensions {
		public static String[] GetClasses(this ScatterData self) {
			return Regex.Split(self.Class, "\\s+");
		}
		public static String[] GetClasses(this ScatterDatum self) {
			return Regex.Split(self.Class, "\\s+");
		}

	}
}