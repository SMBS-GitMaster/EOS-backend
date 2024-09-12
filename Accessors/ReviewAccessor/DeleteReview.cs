using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Linq;

namespace RadialReview.Accessors {
	public partial class ReviewAccessor : BaseAccessor {
		public static void DeleteReviewContainer(UserOrganizationModel caller, long reviewContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					DateTime now = DateTime.UtcNow;
					PermissionsUtility.Create(s, caller).AdminReviewContainer(reviewContainerId);

					var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
					reviewContainer.DeleteTime = now;
					s.Update(reviewContainer);
					var reviews = s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId).List().ToList();
					foreach (var r in reviews) {
						if (r.DeleteTime == null) {
							r.DeleteTime = now;
							s.Update(r);
						}
					}
					var answers = s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId).List().ToList();
					foreach (var a in answers) {
						a.DeleteTime = now;
						s.Update(a);
					}
					tx.Commit();
					s.Flush();
				}
			}
		}
	}
}
