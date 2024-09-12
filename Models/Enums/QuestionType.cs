using RadialReview.Core.Properties;
using RadialReview.Utilities.Attributes;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.Enums {
	public enum QuestionType {
		[Display(Name = "invalid", ResourceType = typeof(DisplayNameStrings)), Icon(BootstrapGlyphs.minus_sign)]
		Invalid = 0,
		[Display(Name = "relativeComparison", ResourceType = typeof(DisplayNameStrings)), Icon(BootstrapGlyphs.transfer)]
		RelativeComparison = 1,
		[Display(Name = "slider", ResourceType = typeof(DisplayNameStrings)), Icon(BootstrapGlyphs.tasks)]
		Slider = 2,
		[Display(Name = "thumbs", ResourceType = typeof(DisplayNameStrings)), Icon(BootstrapGlyphs.thumbs_up)]
		Thumbs = 3,
		[Display(Name = "feedback", ResourceType = typeof(DisplayNameStrings)), Icon(BootstrapGlyphs.pencil)]
		Feedback = 4,
		[Display(Name = "GWC"), Icon(BootstrapGlyphs.pencil)]
		GWC = 5,
		[Display(Name = "Goal"), Icon(BootstrapGlyphs.pencil)]
		Rock = 6,
		[Display(Name = "CompanyValue"), Icon(BootstrapGlyphs.pencil)]
		CompanyValue = 7,
		[Display(Name = "Radio"), Icon(BootstrapGlyphs.pencil)]
		Radio = 8


	}
}
