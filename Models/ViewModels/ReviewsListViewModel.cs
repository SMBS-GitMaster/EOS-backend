using RadialReview.Models.Interfaces;
using System.Collections.Generic;

namespace RadialReview.Models.ViewModels {
	public class ReviewsListViewModel : IPagination {
		public UserOrganizationModel ForUser { get; set; }
		public List<ReviewModel> Reviews { get; set; }

		public double NumPages { get; set; }

		public int Page { get; set; }
	}
}
