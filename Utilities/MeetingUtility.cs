using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.L10.VM;
using System.Collections.Generic;

namespace RadialReview.Utilities {
	public static class MeetingUtility {
	
		public static string GetPageTitle(UserOrganizationModel userOrganization, L10MeetingVM model) { 

			if(model is null)
				return string.Empty;

			if(string.IsNullOrEmpty(model.SelectedPageId))
				return string.Empty;

			if(model.Recurrence is null)
				return string.Empty;

			long.TryParse(model.SelectedPageId.SubstringAfter("-"), out long pageId);

			return L10Accessor.GetPageInRecurrence(userOrganization, pageId, model.Recurrence.Id).Title;
		}

		public static string GetPageTypeMapping(string pageType)
        {
			Dictionary<string, string> typesMap = new Dictionary<string, string>
			{
				{"segue", "Check-in" },
				{"rocks", "Goals" },
				{"scorecard", "Metrics" },
				{"todo", "To-dos" },
				{"ids", "Issues" },
				{"conclude", "Wrap-up" }
			};

			return typesMap.GetValueOrDefault(pageType.ToLower()) ?? pageType;
		}
	}
}