using Microsoft.AspNetCore.Http;
using RadialReview.Models.ViewModels.Application;
using static RadialReview.Middleware.Request.HttpContextExtensions.HttpContextItems;

namespace RadialReview.Middleware.Request.HttpContextExtensions.Navbar {
	public static class HttpContextItemsNavBar {
		public static HttpContextItemKey NAVBAR = new HttpContextItemKey("Navbar");

		public static NavBarViewModel GetNavBar(this HttpContext ctx) {
			return ctx.GetOrCreateRequestItem(NAVBAR, x => new NavBarViewModel());
		}
	}
}
