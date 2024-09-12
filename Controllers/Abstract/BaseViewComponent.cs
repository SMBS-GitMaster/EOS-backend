using Microsoft.AspNetCore.Mvc;
using RadialReview.Middleware.Request.HttpContextExtensions;
using RadialReview.Models;
using RadialReview.Utilities.NHibernate;

namespace RadialReview.Controllers {
	public abstract class BaseViewComponent : ViewComponent {
		protected UserOrganizationModel GetUser() {
			return HttpContext.GetUser();
		}
		
	}
}
