using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.Enums {
	public enum PeopleHeadlineType {
		[Display(Name = "None")]
		None = 0,
		[Display(Name = "Box")]
		HeadlinesBox = 1,
		[Display(Name = "List")]
		HeadlinesList = 2,
	}
}