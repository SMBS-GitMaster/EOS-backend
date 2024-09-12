using RadialReview.Models.Template;
using System.Collections.Generic;

namespace RadialReview.Models.ViewModels {
	public class TemplateViewModel {
		public string Name { get; set; }

		public List<QuestionCategoryModel> Categories { get; set; }

		public List<TemplateItem> Items { get; set; }

	}
}