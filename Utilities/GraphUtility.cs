using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities {
	public class GraphUtility {

		public class Node {
			public long Id { get; set; }
			public long? ParentId { get; set; }
		}

		/// <summary>
		/// https://en.wikipedia.org/wiki/Topological_sorting
		/// </summary>
		/// <param name="nodes"></param>
		/// <returns></returns>
		public static bool HasCircularDependency<T>(List<T> nodes, Func<T, long> idFunc, Func<T, long?> parentIdFunc) {
			var nodeIds = nodes.Select(x => idFunc(x)).Distinct().ToList();
			var edges = nodes.Where(x => parentIdFunc(x).HasValue).Select(x => Tuple.Create(idFunc(x), parentIdFunc(x).Value)).ToList();
			return TopologicalSort(new HashSet<long>(nodeIds), new HashSet<Tuple<long, long>>(edges)) == null;
		}


		public static bool HasCircularDependency(List<Node> nodes) {
			return HasCircularDependency(nodes, x => x.Id, x => x.ParentId);
		}

		/// <summary>
		/// https://gist.github.com/Sup3rc4l1fr4g1l1571c3xp14l1d0c10u5/3341dba6a53d7171fe3397d13d00ee3f/
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="nodes"></param>
		/// <param name="edges"></param>
		/// <returns></returns>
		public static List<T> TopologicalSort<T>(HashSet<T> nodes, HashSet<Tuple<T, T>> edges) where T : IEquatable<T> {
      //Clone it first
      edges = new HashSet<Tuple<T,T>>(edges.Select(x => x).ToList());
			// Empty list that will contain the sorted elements
			var L = new List<T>();

			// Set of all nodes with no incoming edges
			var S = new HashSet<T>(nodes.Where(n => edges.All(e => e.Item2.Equals(n) == false)));

			// while S is non-empty do
			while (S.Any()) {

				//  remove a node n from S
				var n = S.First();
				S.Remove(n);

				// add n to tail of L
				L.Add(n);
				// for each node m with an edge e from n to m do
				foreach (var e in edges.Where(e => e.Item1.Equals(n)).ToList()) {
					var m = e.Item2;
					// remove edge e from the graph
					edges.Remove(e);

					// if m has no other incoming edges then
					if (edges.All(me => me.Item2.Equals(m) == false)) {
						// insert m into S
						S.Add(m);
					}
				}
			}

			// if graph has edges then
			if (edges.Any()) {
				// return error (graph has at least one cycle)
				return null;
			} else {
				// return L (a topologically sorted order)
				return L;
			}
		}

		public struct GraphContext<T> {
			public GraphContext(int depth, int order, T node) {
				Depth = depth;
				Order = order;
				Node = node;
			}

			public int Depth { get; set; }
			public int Order { get; set; }
			public T Node { get; set; }
		}

		public static void BFS<T>(T start, Func<T, IEnumerable<T>> children, Action<GraphContext<T>> action, int maxDepth = int.MaxValue) {
			var seen = new HashSet<T>();
			var order = new Dictionary<int, int>();
			BFS(start, children, action, seen, order, maxDepth);
		}
		private static void BFS<T>(T start, Func<T, IEnumerable<T>> children, Action<GraphContext<T>> action, HashSet<T> visited, Dictionary<int, int> order, int maxDepth) {

			var queue = new Queue<GraphContext<T>>();
			queue.Enqueue(new GraphContext<T>(0, 0, start));

			while (queue.Count > 0) {
				var vertex = queue.Dequeue();

				if (visited.Contains(vertex.Node))
					continue;

				action(vertex);

				foreach (var neighbor in children(vertex.Node))
					if (!visited.Contains(neighbor)) {
						var d = vertex.Depth + 1;

						if (d <= maxDepth) {
							if (!order.ContainsKey(d))
								order.Add(d, 0);
							order[d] += 1;
							queue.Enqueue(new GraphContext<T>(d, order[d], neighbor));
						}
					}
			}
		}
	}
}
