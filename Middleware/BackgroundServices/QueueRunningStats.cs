using System;
using System.Collections.Concurrent;

namespace RadialReview.Middleware.BackgroundServices {
	public class QueueRunningStats {
		public QueueRunningStats(TimeSpan range) {
			Range = range;
		}

		private ConcurrentQueue<DateTime> ActionTimes = new ConcurrentQueue<DateTime>();

		public int Count { get { return ActionTimes.Count; } }

		public DateTime Start { get { return End - Range; } }
		public DateTime End { get { return DateTime.UtcNow; } }
		public TimeSpan Range { get; set; }


		public void LogAction() {
			ActionTimes.Enqueue(DateTime.UtcNow);
		}

		private int[] GetHistogram(int buckets) {
			var queue = ActionTimes;
			var hist = new int[Math.Max(1, buckets)];
			var now = DateTime.UtcNow;
			foreach (var q in queue.ToArray()) {
				double percent = ((now - q) / Range);
				int idx = (int)Math.Floor(hist.Length * percent);
				hist[Math.Clamp(idx, 0, hist.Length - 1)] += 1;
			}
			return hist;
		}

		private string BAR_SET =  "▁▂▃▄▅▆▇█";
		private string DOT_SET = "⡀⡄⡆⡇";

		public string HistogramToString(int buckets = 60) {
			Clean();
			var hist = GetHistogram( buckets);
			var chars = BAR_SET;
			var max = hist.MaxOrDefault(x => x, 0);
			var builder = "";
			foreach (var h in hist) {
				if (h == 0) {
					builder = " " + builder;
				} else {
					double perc = (double)h / max;
					int i = (int)Math.Clamp(Math.Round(chars.Length * perc), 0, chars.Length - 1);
					builder = chars[i] + builder;
				}
			}
			return "|" + builder + "|";
		}


		public void Clean() {
			var queue = ActionTimes;
			DateTime now = DateTime.UtcNow;
			DateTime last;
			while (queue.TryPeek(out last) && now - last > Range) {
				queue.TryDequeue(out _);
			}
		}

	}

}
