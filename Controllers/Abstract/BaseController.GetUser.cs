using RadialReview.Models;
using Microsoft.AspNetCore.Mvc;
using RadialReview.Middleware.Request.HttpContextExtensions;

namespace RadialReview.Controllers {
	public partial class BaseController : Controller {

		private UserOrganizationModel MockUser = null;

		protected UserModel GetUserModel() {
			return HttpContext.GetUserModel();
		}
		protected UserModel GetUserModelAndStyles() {
			return HttpContext.GetUserModel(true);
		}
		protected UserOrganizationModel GetUser() {
			return HttpContext.GetUser();
		}
	}
}
