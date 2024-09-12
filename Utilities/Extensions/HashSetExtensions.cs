using System.Collections.Generic;

namespace RadialReview {
	public static class HashSetExtensions {
		public static HashSet<T> AddIf<T>(this HashSet<T> self, T toAdd, bool addIf) {
			if (addIf) {
				self.Add(toAdd);
			}
			return self;
		}
	}
}