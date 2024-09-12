using RadialReview.Models;
using RadialReview.Models.Askables;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using RadialReview.Models.Reviews;
using NHibernate;

namespace RadialReview.Accessors {
	public class AskableAccessor : BaseAccessor {

		public static List<Askable> GetAskablesForUser(UserOrganizationModel caller, Reviewee forReviewee, DateRange range) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					return GetAskablesForUser(s, s.ToQueryProvider(true), perm, forReviewee, range);
				}
			}
		}
		public static List<Askable> GetAskablesForUser(ISession s, AbstractQuery queryProvider, PermissionsUtility perms, Reviewee reviewee, DateRange range) {
			var allAskables = new List<Askable>();

			var rgm = queryProvider.Get<ResponsibilityGroupModel>(reviewee.RGMId);
			if (rgm == null || rgm.Organization == null)
				return allAskables;

			var orgId = rgm.Organization.Id;

			if (rgm is OrganizationModel) {
				allAskables.AddRange(OrganizationAccessor.AskablesAboutOrganization(queryProvider, perms, orgId, range));
			} else if (rgm is UserOrganizationModel) {
				allAskables.AddRange(ApplicationAccessor.GetApplicationQuestions(queryProvider));
				allAskables.AddRange(ResponsibilitiesAccessor.GetResponsibilitiesForUser(queryProvider, perms, reviewee.RGMId, range));
				allAskables.AddRange(QuestionAccessor.GetQuestionsForUser(queryProvider, perms, reviewee.RGMId, range));
				allAskables.AddRange(RockAccessor.GetRocksForUser(queryProvider, perms, reviewee.RGMId, range));
				allAskables.AddRange(RoleAccessor.Deprecated.GetRolesForReviewee(s,queryProvider, perms, reviewee, range));
				allAskables.AddRange(OrganizationAccessor.GetCompanyValues(queryProvider, perms, orgId, range));
			}

			return allAskables.ToList();
		}
	}
}
