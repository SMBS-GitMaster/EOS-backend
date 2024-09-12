using RadialReview.Models.Angular.Base;
using System.Collections.Generic;

namespace RadialReview.Models.Angular.Accountability {
	public class AngularAccountabilityChartSearch : BaseAngular {
		public AngularAccountabilityChartSearch(long id) : base(id) {
		}
		public Dictionary<long, string> searchPos { get; set; }
	}
}