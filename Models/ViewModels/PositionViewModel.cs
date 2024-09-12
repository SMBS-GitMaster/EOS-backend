using RadialReview.Models.Askables;
using System;
using System.Collections.Generic;

namespace RadialReview.Models.ViewModels {
	public class PositionViewModel {
		public long Id { get; set; }
		public List<PositionModel> Positions { get; set; }

		public long? Position { get; set; }

		public String PositionName { get; set; }
	}
}