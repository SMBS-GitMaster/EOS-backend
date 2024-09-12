using System.Collections.Generic;

namespace RadialReview.Models.ViewModels {
	public class ManagerUserViewModel {
		public UserOrganizationModel User { get; set; }
		public List<QuestionModel> MatchingQuestions { get; set; }

		public long OrganizationId { get; set; }

	}
}