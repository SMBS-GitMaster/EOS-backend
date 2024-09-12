using Newtonsoft.Json;
using System.Collections.Generic;

namespace RadialReview.Models.Charts {
	public class Line {
		public long start { get; set; }
		public long end { get; set; }
		[JsonIgnore]
		public List<LineChart> charts { get; set; }
		public long? marginTop { get; set; }
		public long? marginRight { get; set; }
		public long? marginBottom { get; set; }
		public long? marginLeft { get; set; }
		public List<LinePoint> values { get; set; }

		public Line() {
			charts = new List<LineChart>();
		}

		public class LineChart {
			public bool rounding { get; set; }
			public string color { get; set; }
			public string axis { get; set; }
			public string name { get; set; }
			public string displayName { get; set; }
			public List<LinePoint> values { get; set; }
			public LineChart() {
				values = new List<LinePoint>();
			}
		}

		public class LinePoint {
			public long time { get; set; }
			public decimal? value { get; set; }
		}
	}
}
