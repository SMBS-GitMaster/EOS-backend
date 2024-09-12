using NHibernate;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Documents.Interceptors {
	public static class DocumentFolderInterceptorHelpers {
		public static List<UserOrganizationModel> GetMyUserIdAtOrganization(this IDocumentFolderInterceptor_Unsafe interceptor, ISession s, UserOrganizationModel caller, long orgId) {
			if (caller.Organization.Id == orgId && caller.UserIds.Length == 1) {
				return new List<UserOrganizationModel>() { caller };
			} else {
				var allIds = caller.UserIds.ToArray();
				return s.QueryOver<UserOrganizationModel>()
					.Where(x => x.DeleteTime == null && x.Organization.Id == orgId)
					.WhereRestrictionOn(x => x.Id).IsIn(allIds)
					.List()
					.ToList();
			}
		}
	}
}