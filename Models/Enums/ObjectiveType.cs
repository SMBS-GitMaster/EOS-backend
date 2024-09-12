using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.Enums {
	public enum ObjectiveType {
		[Display(Name = "None")]
		None = 0,
		[Display(Name = "Linear")]
		Linear = 1,
		[Display(Name = "Linear (Adjusting)")]
		LinearAdjusting = 2,


	}
}