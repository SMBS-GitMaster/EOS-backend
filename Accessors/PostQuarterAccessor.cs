using System.Linq;
using RadialReview.Models;
using RadialReview.Models.PostQuarter;
using RadialReview.Utilities;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public class PostQuarterAccessor {
		public static async Task<PostQuarterModel> CreatePostQuarter(UserOrganizationModel caller, PostQuarterModel postQuarter) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).EditL10Recurrence(postQuarter.L10RecurrenceId);
					var existingPostQuarter = s.QueryOver<PostQuarterModel>().Where(x => x.DeleteTime == null && x.L10RecurrenceId == postQuarter.L10RecurrenceId && x.OrganizationId == caller.Organization.Id && x.QuarterEndDate == postQuarter.QuarterEndDate.Date).List().FirstOrDefault();
					if (existingPostQuarter != null) {
						postQuarter.Id = existingPostQuarter.Id;
						existingPostQuarter.Name = postQuarter.Name;
						s.Update(existingPostQuarter);
					} else {
						postQuarter.OrganizationId = caller.Organization.Id;
						postQuarter.CreatedBy = caller.Id;
						s.Save(postQuarter);
					}

					tx.Commit();
					s.Flush();
					return postQuarter;
				}
			}
		}
	}
}