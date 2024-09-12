using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview {
	public static class PaginationExtensions {
		public static int PageCount<T>(this IEnumerable<T> self, int resultPerPage) {
			return (int)Math.Ceiling(self.Count() / ((double)resultPerPage));
		}
		public static IEnumerable<T> Paginate<T>(this IEnumerable<T> self, int page, int resultPerPage) {
			return self.Skip(page * resultPerPage).Take(resultPerPage);
		}		
	}
}