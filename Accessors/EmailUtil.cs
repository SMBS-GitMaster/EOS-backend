using RadialReview.Models;
using RadialReview.Models.UserModels;
using System.Linq;

namespace RadialReview.Utilities {
	public class EmailUtil {
		public static string GuessEmail(UserOrganizationModel caller, long organizationId, string firstName, string lastName) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(organizationId);

					var data = s.QueryOver<UserLookup>()
						.Where(x => x.OrganizationId == organizationId && x.DeleteTime == null)
						.Select(x => x.Name, x => x.Email)
						.List<object[]>()
						.Select(x => {
							var name = ((string)x[0]).Split(' ');
							var email = (string)x[1];

							var fn = name.FirstOrDefault();
							var ln = name.Take(1).LastOrDefault();

							return new {
								first = fn,
								last = ln,
								email
							};
						}).ToList();




				}
			}


			return null;
		}
	}
}