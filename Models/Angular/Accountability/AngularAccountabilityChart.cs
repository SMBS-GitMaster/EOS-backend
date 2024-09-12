using RadialReview.Exceptions;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Users;
using System;
using System.Collections.Generic;

namespace RadialReview.Models.Angular.Accountability {
	public class AngularAccountabilityChart : BaseAngular {

		public AngularAccountabilityChart() {
		}
		public AngularAccountabilityChart(long id) : base(id) {
			search = new AngularAccountabilityChartSearch(id);
		}
		public AngularAccountabilityNode Root { get; set; }
		public IEnumerable<AngularUser> AllUsers { get; set; }
		public long? CenterNode { get; set; }
		public long? ShowNode { get; set; }
		public long? Selected { get; set; }
		public long? ExpandNode { get; set; }
		public bool? CanReorganize { get; set; }
		public AngularAccountabilityChartSearch search { get; set; }

		public AngularAccountabilityNode FindFirst(Predicate<AngularAccountabilityNode> match) {
			AngularAccountabilityNode found = null;
			try {
				Dive(x => {
					if (match(x)) {
						found = x;
						throw new BreakEarlyException();
					}

				});
			} catch (BreakEarlyException e) {
			}
			return found;
		}

		public void Dive(Action<AngularAccountabilityNode> action) {
			Dive(action, Root);
		}
		protected void Dive(Action<AngularAccountabilityNode> action, AngularAccountabilityNode node) {
			if (node != null) {
				action(node);

				var children = node.GetDirectChildren();
				if (children != null) {
					foreach (var c in children) {
						Dive(action, c);
					}
				}
			}
		}
		public void ToggleExpand(bool expand) {
			if (expand) {
				ExpandAll();
			} else {
				CollapseAll();
			}
		}
		public void ExpandAll() {
			if (Root != null) {
				Root.ExpandAll();
			}
		}
		public void CollapseAll() {
			if (Root != null) {
				Root.CollapseAll();
			}
		}
		public void SetCenterNode(long? nodeId) {
			CenterNode = nodeId;
			if (Root != null && nodeId != null) {
				Root.ShowNode(nodeId.Value);
			}
		}
		public void ShowUser(long? userId) {
			if (Root != null && userId != null) {
				Root.ShowUser(userId.Value);
			}
		}

		public void ExpandLevels(int levels) {
			if (Root != null) {
				Root.ExpandLevels(levels);
			}
		}
	}
}
