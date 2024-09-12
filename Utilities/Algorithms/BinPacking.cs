using System.Collections.Generic;
namespace RadialReview.Utilities.BinPacking {

	/// <summary>
	/// https://github.com/davidmchapman/3DContainerPacking
	/// </summary>
	public class RectangleBinPack<T> {

		public class Node {
			internal Node left;
			internal Node right;
			internal double width;
			internal double height;
			internal double x;
			internal double y;
			private double w;
			private double h;
			public T Object { get; set; }
			public Node() {
			}
			public Node(T obj, double w, double h) {
				this.w = w;
				this.h = h;
				Object = obj;
			}
		}

		public double binWidth { get; set; }
		public double binHeight { get; set; }
		public Node root { get; set; }

		/** Restarts the packing process, clearing all previously packed rectangles and
			sets up a new bin of a given initial size. These bin dimensions stay fixed during
			the whole packing process, i.e. to change the bin size, the packing must be
			restarted again with a new call to Init(). */
		public RectangleBinPack(int width, int height) {
			binWidth = width;
			binHeight = height;
			root = new Node();
			root.left = null;
			root.right = null;
			root.x = 0;
			root.y = 0;
		}

		/** Recursively calls itself. */
		public double UsedSurfaceArea(Node node) {
			if (node.left != null || node.right != null) {
				double usedSurfaceArea = node.width * node.height;
				if (node.left != null)
					usedSurfaceArea += UsedSurfaceArea(node.left);
				if (node.right != null)
					usedSurfaceArea += UsedSurfaceArea(node.right);

				return usedSurfaceArea;
			}

			// This is a leaf node, it doesn't constitute to the total surface area.
			return 0;
		}

		/** Running time is linear to the number of rectangles already packed. Recursively calls itself.
			@return 0null If the insertion didn't succeed. */
		public Node Insert(Node node, double width, double height) {
			// If this node is an internal node, try both leaves for possible space.
			// (The rectangle in an internal node stores used space, the leaves store free space)
			if (node.left != null || node.right != null) {
				if (node.left != null) {
					var newNode = Insert(node.left, width, height);
					if (newNode != null)
						return newNode;
				}
				if (node.right != null) {
					var newNode = Insert(node.right, width, height);
					if (newNode != null)
						return newNode;
				}
				return null; // Didn't fit into either subtree!
			}

			// This node is a leaf, but can we fit the new rectangle here?
			if (width > node.width || height > node.height)
				return null; // Too bad, no space.

			// The new cell will fit, split the remaining space along the shorter axis,
			// that is probably more optimal.
			double w = node.width - width;
			double h = node.height - height;
			node.left = new Node();
			node.right = new Node();
			if (w <= h) // Split the remaining space in horizontal direction.
			{
				node.left.x = node.x + width;
				node.left.y = node.y;
				node.left.width = w;
				node.left.height = height;

				node.right.x = node.x;
				node.right.y = node.y + height;
				node.right.width = node.width;
				node.right.height = h;
			} else {// Split the remaining space in vertical direction.

				node.left.x = node.x;
				node.left.y = node.y + height;
				node.left.width = width;
				node.left.height = h;

				node.right.x = node.x + width;
				node.right.y = node.y;
				node.right.width = w;
				node.right.height = node.height;
			}
			// Note that as a result of the above, it can happen that node->left or node->right
			// is now a degenerate (zero area) rectangle. No need to do anything about it,
			// like remove the nodes as "unnecessary" since they need to exist as children of
			// this node (this node can't be a leaf anymore).

			// This node is now a non-leaf, so shrink its area - it now denotes
			// *occupied* space instead of free space. Its children spawn the resulting
			// area of free space.
			node.width = width;
			node.height = height;
			return node;
		}

		public List<Node> Contents() {
			return Contents(root);
		}

		private List<Node> Contents(Node n) {
			var contents = new List<Node>();
			if (n != null) {
				if (n.left != null) {
					contents.Add(n.left);
					contents.AddRange(Contents(n.left));
				}
				if (n.right != null) {
					contents.Add(n.right);
					contents.AddRange(Contents(n.right));
				}
			}
			return contents;
		}
	}







































































































































































































































}
