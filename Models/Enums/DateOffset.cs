using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.Enums {
	public enum DateOffset {
		[Display(Name = "Not set")]
		Invalid = 0,
		[Display(Name = "First of the month")]
		FirstOfMonth = 1,
		[Display(Name = "First Monday of the month")]
		FirstMondayOfTheMonth = 3,
		[Display(Name = "First Sunday of the month")]
		FirstSundayOfTheMonth = 4,
		[Display(Name = "Monday of the 4th week")]
		MondayOfFourthWeek = 5,

	}
}
