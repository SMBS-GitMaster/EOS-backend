using RadialReview.Models.Askables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.ViewModels {
	public class OrgPositionsViewModel {
		public List<OrgPosViewModel> Positions { get; set; }

		public bool CanEdit { get; set; }

	}

	public class OrgPosViewModel {
		public long Id { get; set; }
		public String Name { get; set; }
		public String SimilarTo { get; set; }
		public int NumAccountabilities { get; set; }
		public int NumPeople { get; set; }
		public long? TemplateId { get; set; }

		public OrgPosViewModel(string name, int numPeople) {
			Id = -1;
			Name = name;
			NumPeople = numPeople;
			NumAccountabilities = 0;
			TemplateId = null;
		}


		[Obsolete("do not use", true)]
		public OrgPosViewModel(Deprecated.OrganizationPositionModel model, int numPeople) {
			Id = model.Id;
			Name = model.CustomName;
			NumAccountabilities = model.Responsibilities.Count();
			NumPeople = numPeople;
			TemplateId = model.TemplateId;
		}


	}
}
