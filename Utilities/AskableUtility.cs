using RadialReview.Accessors;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Reviews;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace RadialReview.Utilities {
	public class AskableCollection : IEnumerable<AskableAbout> {

		public List<AskableAbout> Askables { get; set; }
		public List<Reviewer> Reviewers { get; set; }
		public Dictionary<Reviewer, AboutType> RevieweeIsThe { get; set; }

		public AskableCollection() {
			Askables = new List<AskableAbout>();
			Reviewers = new List<Reviewer>();
			RevieweeIsThe = new Dictionary<Reviewer, AboutType>();
		}

		public void AddUnique(Askable askable, AboutType reviewerIsThe, Reviewee reviewee) {
			if (ReviewAccessor.ShouldAddToReview(askable, reviewerIsThe)) {
				foreach (var a in Askables) {
					if (a.Reviewee == reviewee && a.Askable.Id == askable.Id) {
						a.ReviewerIsThe = a.ReviewerIsThe | reviewerIsThe;
						return;
					}
				}
				Askables.Add(new AskableAbout(askable, reviewee, reviewerIsThe));
			}
		}

		public void AddUnique(Askable askable, AccRelationship relationship) {
			AddUnique(askable, relationship.ReviewerIsThe, relationship.Reviewee);
			if (!Reviewers.Any(x => x == relationship.Reviewer)) {
				Reviewers.Add(relationship.Reviewer);
				RevieweeIsThe[relationship.Reviewer] = AboutType.NoRelationship;
			}
			RevieweeIsThe[relationship.Reviewer] = RevieweeIsThe[relationship.Reviewer] | relationship.RevieweeIsThe;
		}

		public void AddAll(List<Askable> revieweeQuestions, AboutType reviewerIsThe, Reviewee reviewee) {
			foreach (var askable in revieweeQuestions) {
				AddUnique(askable, reviewerIsThe, reviewee);
			}
		}

		public void AddAll(List<Askable> revieweeQuestions, AccRelationship relationship) {
			foreach (var askable in revieweeQuestions) {
				AddUnique(askable, relationship);
			}
		}

		public IEnumerator<AskableAbout> GetEnumerator() {
			return Askables.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator() {
			return Askables.GetEnumerator();
		}
	}


}
