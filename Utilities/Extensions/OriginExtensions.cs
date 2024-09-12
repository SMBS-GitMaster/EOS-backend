using RadialReview.Models;
using RadialReview.Models.Interfaces;

namespace RadialReview {
	public static class OriginExtensions {
		public static Origin GetOrigin(this IOrigin self) {
			return new Origin(self.GetOriginType(), self.Id);
		}
	}
}