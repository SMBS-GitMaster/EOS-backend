using System.Collections.Concurrent;

namespace RadialReview.Utilities.DataTypes {
	public class CircularQueue<T> : ConcurrentQueue<T> {
		private readonly object syncObject = new object();

		public int Size { get; private set; }

		public CircularQueue(int size) {
			Size = size;
		}

		public new void Enqueue(T obj) {
			base.Enqueue(obj);
			lock (syncObject) {
				while (base.Count > Size) {
					T outObj;
					base.TryDequeue(out outObj);
				}
			}
		}

		public void Empty() {
			lock (syncObject) {
				while (base.Count > 0) {
					T outObj;
					base.TryDequeue(out outObj);
				}
			}
		}
	}
}