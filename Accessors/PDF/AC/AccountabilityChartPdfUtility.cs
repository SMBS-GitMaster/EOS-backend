using RadialReview.Models.Angular.Accountability;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Accessors.PDF.AC {
	public class AccountabilityChartPdfUtility {
		//Emperically found role line width
		private static string MAX_LINE = "WWWWWWWWWWWWWWW";

		//Landscape ratio
		private static double TARGET_RATIO = 11.0 / 8.5;
		private static double BB_ADJ_RATIO = 109.0 / 6.0;

		private static int MAX_WIDTH = 120 * 8; //guess
		private static int MAX_HEIGHT = 70; //guess

		public static IEnumerable<AccountabilityPrintoutSettings> GetAllChildrenCharts(Chart<AngularAccountabilityChart> chart, Predicate<AngularAccountabilityNode> showNode, int targetDepth, int maxDepth = int.MaxValue) {

			//Build a list of all the nodes we want to see.
			var requiredTogether = new Dictionary<long, List<long>>();
			var remainingRequiredNodeIds = new List<long>();
			var depths = new Dictionary<long, int>();

			var nodes = new List<AngularAccountabilityNode>();
			var highlight = new HashSet<long>();

			//Find important nodes. also add to highlight list
			chart.data.Root.ForEach(n => {
				if (showNode(n)) {
					nodes.Add(n);
					highlight.Add(n.Id);
					remainingRequiredNodeIds.Add(n.Id);
				}
			});

			GraphUtility.BFS(chart.data.Root, n => n.GetDirectChildren(), n => { depths[n.Node.Id] = n.Depth; });

			remainingRequiredNodeIds = remainingRequiredNodeIds.Distinct().OrderBy(x => depths[x]).ToList();
			nodes = nodes.OrderBy(x => depths[x.Id]).ToList();

			//Get list of node ids we expect to see given target depth
			foreach (var found in nodes) {
				if (found != null) {
					requiredTogether[found.Id] = new List<long>();
					GraphUtility.BFS(found, n => n.GetDirectChildren(), x => {
						//only add if not too deep.
						if (x.Depth <= targetDepth) {
							requiredTogether[found.Id].Add(x.Node.Id);
							//expected.Add(x);
						}
					});
				}
			}

			//Go through and gather all the charts.
			while (remainingRequiredNodeIds.Any()) {
				//Pop off a node to start from.
				var current = remainingRequiredNodeIds.First();
				var node = nodes.First(x => x.Id == current);

				var remainingIdCount = remainingRequiredNodeIds.Sum(x => requiredTogether[x].Count());
				var remainingNodes = nodes.Where(x => remainingRequiredNodeIds.Contains(x.Id)).ToList();

				if (remainingIdCount > 10 || !DistinctRoots(remainingNodes,requiredTogether)) {
					//For the general case...
					remainingRequiredNodeIds.Remove(current);
					//find the action that produces the best bounding box... see if we can fit a few expected ids in the batch.
					var action = MaximizeBoundingBox(node, MAX_WIDTH, MAX_HEIGHT, maxDepth, requiredTogether, highlight);
					var setting = new AccountabilityPrintoutSettings();
					action.SettingsAction(setting);
					yield return setting;

				} else {
					var roots = remainingNodes;
					var action = MaximizeBoundingBox_Remainder(roots, targetDepth, highlight);
					var setting = new AccountabilityPrintoutSettings();
					action.SettingsAction(setting);
					yield return setting;
					break;
				}

				//remove all that were already shown
				var visibleIds = new HashSet<long>();
				node.ForEachVisible(n => visibleIds.Add(n.Id));

				//remove where requiredTogethers are present.
				foreach (var rt in requiredTogether) {
					if (visibleIds.Contains(rt.Key) && rt.Value.All(x => visibleIds.Contains(x))) {
						remainingRequiredNodeIds.RemoveAll(x => x == rt.Key);
					}
				}
			}
		}

		/// <summary>
		/// Test if the roots are part of any other requiredTogether groups.
		/// </summary>
		private static bool DistinctRoots(List<AngularAccountabilityNode> roots, Dictionary<long, List<long>> requiredTogether) {
			foreach (var root in roots) {
				var rtIds = requiredTogether[root.Id].Where(x=>x!=root.Id);
				var intersect = rtIds.Intersect(roots.Select(r => r.Id));
				if (intersect.Any()) {
					return false;
				}
			}
			return true;
		}

		private static TreeModActionStatus MaximizeBoundingBox_Remainder(List<AngularAccountabilityNode> roots, int targetDepth, HashSet<long> highlight) {

			var action = new TreeModAction(n => {
				//<<----...Ignored...---->>
				n.GetBacking().CollapseAll();
				n.GetBacking().ExpandLevels(targetDepth);
				//<<----...Ignored...---->>
			}, settings => {
				//Isolate the reamining roots.
				settings.IsolateNodes(roots.Select(x => x.Id).ToList(), targetDepth);
				settings.CollaseAll();
				//Assumes that target depth was used to construct the remainTogether variable
				settings.ExpandLevels(targetDepth + 1);
				settings.ShowUserNames(3);
				settings.Highlight(highlight);
				settings.Compactify(false);
				settings.PreparePdfViewport();
			});

			return new TreeModActionStatus(action, true, 1, 1, 0, 0);
		}

		private static TreeModActionStatus MaximizeBoundingBox(AngularAccountabilityNode root, int width, int height, int maxAllowableDepth, Dictionary<long, List<long>> requiredTogether, HashSet<long> highlight) {
			var r = new AAN_Node(root);

			TreeModActionStatus best = null;

			int chartDepth = 0;
			//find max depth.
			GraphUtility.BFS(root, n => n.GetDirectChildren(), c => chartDepth = Math.Max(chartDepth, c.Depth));

			var maxDepth = Math.Min(maxAllowableDepth + 1, chartDepth);

			for (var i = 0; i <= maxDepth; i++) {
				var action = new TreeModAction(n => {
					n.GetBacking().CollapseAll();
					n.GetBacking().ExpandLevels(i);
				}, settings => {
					settings.IsolateNode(root.Id);
					settings.CollaseAll();
					settings.ExpandLevels(i + 1);
					settings.ShowUserNames(3);
					settings.Highlight(highlight);
					settings.Compactify(false);
					settings.PreparePdfViewport();
				});

				var curr = action.ApplyAndTestBoundingBox(r, width, height, requiredTogether);

				if (best == null)
					best = curr;
				if (!curr.Fits)
					break;

				if (curr.IsBetterThan(best)) {
					best = curr;
				}
			}

			best.TreeAction.ApplyAndTestBoundingBox(r, width, height, requiredTogether);

			return best;
		}

		private static int GetTextHeight(string txt, int min) {
			if (txt == null)
				return 0;
			return (int)Math.Max(min, Math.Ceiling((double)txt.Length / (double)MAX_LINE.Length));
		}

		private static int GetTextHeight(IEnumerable<string> lines, int min, int lineMin) {
			if (!lines.Any())
				return min;

			return (int)Math.Max(min, lines.Sum(x => GetTextHeight(x, lineMin)));
		}
		public class AAN_Node : D3.Layout.node<AAN_Node> {
			private void SetBacking(AngularAccountabilityNode node) {
				_backing = node;
				Id = node.Id;
				foreach (var c in node.GetDirectChildren()) {
					_children.Add(new AAN_Node(c));
				}
			}
			public AngularAccountabilityNode GetBacking() {
				return _backing ?? new AngularAccountabilityNode();
			}
			private List<AAN_Node> _children { get; set; }

			public override List<AAN_Node> children {
				get {
					if (!GetBacking().collapsed) {
						return _children;
					} else {
						return new List<AAN_Node>();
					}
				}
				set {
					if (value == null) {
						GetBacking().collapsed = true;
					} else {
						GetBacking().collapsed = false;
						_children = value;
					}
				}
			}

			private AngularAccountabilityNode _backing { get; set; }
			public AAN_Node() {
				_children = new List<AAN_Node>();
			}

			public AAN_Node(AngularAccountabilityNode node) : this() {
				SetBacking(node);
			}

		}


		private class TreeModAction {
			public TreeModAction(Action<AAN_Node> nodeAction, Action<AccountabilityPrintoutSettings> settingsAction) {
				NodeAction = nodeAction;
				SettingsAction = settingsAction;
			}
			private Action<AAN_Node> NodeAction { get; set; }
			public Action<AccountabilityPrintoutSettings> SettingsAction { get; set; }


			public TreeModActionStatus ApplyAndTestBoundingBox(AAN_Node root, int width, int height, Dictionary<long, List<long>> requiredTogether) {
				NodeAction(root);
				return TestBoundingBox(root, width, height, requiredTogether);
			}

			private void GetVisible(AAN_Node node, HashSet<long> visible) {
				visible.Add(node.Id);
				if (!node.GetBacking().collapsed) {
					foreach (var c in node.children) {
						GetVisible(c, visible);
					}
				}
			}


			private TreeModActionStatus TestBoundingBox(AAN_Node root, int width, int height, Dictionary<long, List<long>> requiredTogether) {
				var tree = JS.Tree.Update(root, x => 2.0 + GetTextHeight(x.GetBacking().GetUserNames(), 1) + GetTextHeight(x.GetBacking().Name, 1) + GetTextHeight(x.GetBacking().GetRoles(), 1, 1));

				var visibleIds = new HashSet<long>();
				GetVisible(root, visibleIds);
				var allNodes = tree.nodes(root).ToList();
				var visibleNodes = allNodes.Where(x => visibleIds.Contains(x.Id)).ToList();

				if (!visibleNodes.Any())
					return new TreeModActionStatus(this, true, 1, 1, 0, 0);
				var w = (int)Math.Ceiling(visibleNodes.Max(n => n.x) - visibleNodes.Min(n => n.x));
				var h = (int)Math.Ceiling(visibleNodes.Max(n => n.y) - visibleNodes.Min(n => n.y));

				var visible = 0;
				var requireNodesVisible = 0;

				root.GetBacking().ForEachVisible(n => {
					visible += n.collapsed ? 1 : n.GetDirectChildren().Count;
					visibleIds.Add(n.Id);
				});


				//Count up number of requiredTogethers which are present.
				foreach (var rt in requiredTogether) {
					if (visibleIds.Contains(rt.Key) && rt.Value.All(x => visibleIds.Contains(x))) {
						requireNodesVisible += 1;
					}
				}

				return new TreeModActionStatus(this, w <= width && h <= height, w, h, visible, requireNodesVisible);
			}
		}

		private class TreeModActionStatus {
			public TreeModActionStatus(TreeModAction treeAction, bool fits, int width, int height, int visible, int requireNodesVisible) {
				Fits = fits;
				Width = width;
				Height = height;
				TreeAction = treeAction;
				VisibleCount = visible;
				RequireNodesVisible = requireNodesVisible;
				if (width == 0 && height == 0) {
					Width = 1;
					Height = 1;
				}

			}
			public TreeModAction TreeAction { get; set; }
			public Action<AccountabilityPrintoutSettings> SettingsAction { get { return TreeAction.SettingsAction; } }
			public bool Fits { get; set; }
			public int Width { get; set; }
			public int Height { get; set; }
			public int VisibleCount { get; set; }
			public int RequireNodesVisible { get; set; }

			public bool IsBetterThan(TreeModActionStatus other) {
				if (Fits && !other.Fits)
					return true;
				if (RequireNodesVisible > other.RequireNodesVisible)
					return true;
				var otherRatio = other.Width / (other.Height * BB_ADJ_RATIO);
				var myRatio = Width / (Height * BB_ADJ_RATIO);
				if (double.IsNaN(otherRatio) && !double.IsNaN(myRatio))
					return true;

				//Better fits on a landscape page.
				return Math.Abs(myRatio - TARGET_RATIO) < Math.Abs(otherRatio - TARGET_RATIO);
			}
		}

	}
}
