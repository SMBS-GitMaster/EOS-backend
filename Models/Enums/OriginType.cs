using RadialReview.Core.Properties;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.Enums {

	public enum OriginType {
		[Display(Name = "invalid", ResourceType = typeof(DisplayNameStrings))]
		Invalid,
		[Display(Name = "user", ResourceType = typeof(DisplayNameStrings))]
		User,
		[Display(Name = "group", ResourceType = typeof(DisplayNameStrings))]
		Group,
		[Display(Name = "position", ResourceType = typeof(DisplayNameStrings))]
		Position,
		[Display(Name = "team", ResourceType = typeof(DisplayNameStrings))]
		Team,
		[Display(Name = "organization", ResourceType = typeof(DisplayNameStrings))]
		Organization,
		[Display(Name = "industry", ResourceType = typeof(DisplayNameStrings))]
		Industry,
		[Display(Name = "application", ResourceType = typeof(DisplayNameStrings))]
		Application,


	}
}