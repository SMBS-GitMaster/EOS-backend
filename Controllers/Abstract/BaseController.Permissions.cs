using RadialReview.Exceptions;
using RadialReview.Models;
using Microsoft.AspNetCore.Mvc;

namespace RadialReview.Controllers {
	public partial class BaseController : Controller {
		

		protected void ManagerAndCanEditOrException(UserOrganizationModel user) {
			if (!user.IsManagerCanEditOrganization()) {
				throw new PermissionsException();
			}
		}

	}
}
