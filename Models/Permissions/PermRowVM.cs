using RadialReview.Models.ViewModels;
using RadialReview.Utilities.Permissions.Accessors;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Permissions {

	public class PermRowVM {
		public long Id { get; private set; }
		public bool CanView { get; private set; }
		public bool CanEdit { get; private set; }
		public bool CanAdmin { get; private set; }
		public PermRowSettings RowSettings { get; set; }
		public string Title { get; set; }
		public PictureViewModel Picture { get; set; }

		public PermRowVM(PermItem permItem, PermRowSettings settings) {
			Id = permItem.Id;
			CanView = permItem.CanView;
			CanEdit = permItem.CanEdit;
			CanAdmin = permItem.CanAdmin;
			RowSettings = settings ?? new PermRowSettings();
			ChildrenQueries = new List<IEnumerable<PermRowVM>>();
		}



		public void AddChildren(IEnumerable<PermRowVM> children) {
			ResolvedChildren = null;
			ChildrenQueries.Add(children);
		}

		public IEnumerable<PermRowVM> ResolveChildren() {
			if (ResolvedChildren != null) {
				foreach (var c in ResolvedChildren) {
					c.ResolveChildren();
				}
				return ResolvedChildren;
			} else {
				var children = ChildrenQueries.SelectMany(x => x).ToList();
				foreach (var c in children) {
					c.ResolveChildren();
				}
				ResolvedChildren = children;
				return ResolvedChildren;
			}
		}

		private List<PermRowVM> ResolvedChildren { get; set; }
		private List<IEnumerable<PermRowVM>> ChildrenQueries { get; set; }

	}
}
