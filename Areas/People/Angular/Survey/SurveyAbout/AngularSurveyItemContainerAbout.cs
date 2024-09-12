using System.Collections.Generic;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Angular.Base;
using System.Linq;

namespace RadialReview.Areas.People.Angular.Survey {
	public class AngularSurveyItemContainerAbout : BaseAngular, IItemContainerAbout {

		public AngularSurveyItemContainerAbout() { }
		public AngularSurveyItemContainerAbout(long id) : base(id) { }




		public static AngularSurveyItemContainerAbout ConstructShallow(IItemContainer container) {
			return new AngularSurveyItemContainerAbout(container.Id) {
				Name = container.GetName(),
				Ordering = container.GetOrdering(),
				Help = container.GetHelp(),
				ItemMergerKey = container.GetItemMergerKey(),
				Item = (container.GetItem() == null) ? null : new AngularSurveyItem(container.GetItem()),
				ItemFormat = (container.GetFormat() == null) ? null : new AngularSurveyItemFormat(container.GetFormat()),
				Responses = new List<AngularSurveyResponse>()
			};
		}


		public string Name { get; set; }
		public string Help { get; set; }
		public int? Ordering { get; set; }

		public AngularSurveyItem Item { get; set; }
		public ICollection<AngularSurveyResponse> Responses { get; set; }
		public AngularSurveyItemFormat ItemFormat { get; set; }
		public string ItemMergerKey { get; set; }


		public IItem GetItem() {
			return Item;
		}

		public IItemFormat GetFormat() {
			return ItemFormat;
		}

		public bool HasResponse() {
			return Responses != null && Responses.Any();
		}

		public string GetName() {
			return Name;
		}

		public string GetHelp() {
			return Help;
		}

		public int GetOrdering() {
			return Ordering ?? 0;
		}

		public string ToPrettyString() {
			return "";
		}

		public IEnumerable<IResponse> GetResponses() {
			return Responses;
		}

		public string GetItemMergerKey() {
			return ItemMergerKey;
		}
	}

}