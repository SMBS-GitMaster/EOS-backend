using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Documents {
	public class DocumentItemSortedGroupingVM {
		public string GroupTitle { get; set; }
		public List<DocumentItemVM> Contents { get; set; }

		public static DocumentItemSortedGroupingVM CreateFromGroup<T, SORT>(IGrouping<T, DocumentItemVM> group, Func<DocumentItemVM, SORT> sortBy, bool asc, Func<T, string> keyConvert = null) {
			var res = new DocumentItemSortedGroupingVM();
			if (keyConvert != null) {
				res.GroupTitle = keyConvert(group.Key);
			} else {
				res.GroupTitle = "" + group.Key;
			}
			if (asc) {
				res.Contents = group.OrderBy(sortBy).ToList();
			} else {
				res.Contents = group.OrderByDescending(sortBy).ToList();
			}
			return res;
		}
		public static List<DocumentItemSortedGroupingVM> CreateFromGroup<T, SORT>(IEnumerable<IGrouping<T, DocumentItemVM>> groups, Func<DocumentItemVM, SORT> sortBy, bool asc, Func<T, string> keyConvert = null) {
			var o = groups.Select(x => CreateFromGroup(x, sortBy, asc, keyConvert)).ToList();
			if (asc)
				return o.OrderBy(x => sortBy(x.Contents.First())).ToList();
			else
				return o.OrderByDescending(x => sortBy(x.Contents.First())).ToList();
		}
	}
}