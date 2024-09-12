using FluentNHibernate.Mapping;

namespace RadialReview.Models.Template {
	public class TemplateItemFeedback : TemplateItem {
	}

	public class TemplateItemFeedbackMap : SubclassMap<TemplateItemFeedback> {
		public TemplateItemFeedbackMap() {
		}
	}
}