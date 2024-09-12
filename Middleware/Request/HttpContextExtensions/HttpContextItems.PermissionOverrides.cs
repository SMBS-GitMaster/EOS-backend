using Microsoft.AspNetCore.Http;
using static RadialReview.Middleware.Request.HttpContextExtensions.HttpContextItems;
using static RadialReview.Models.UserOrganizationModel;

namespace RadialReview.Middleware.Request.HttpContextExtensions.Permissions {
	public static class HttpContextItemsPermissions {
		private static HttpContextItemKey IS_RADIAL_ADMIN = new HttpContextItemKey("IsRadialAdmin");
		private static HttpContextItemKey ADMIN_SHORT_CIRCUIT = new HttpContextItemKey("AdminShortCircuit");
		private static HttpContextItemKey PERMISSIONS_OVERRIDES = new HttpContextItemKey("PermissionsOverrides");


		public static PermissionsOverrides GetPermissionOverrides(this HttpContext ctx) {
			return ctx.GetOrCreateRequestItem(PERMISSIONS_OVERRIDES, x=> new PermissionsOverrides());
		}


		public static void SetAdminShortCircuit(this HttpContext ctx, bool adminShortCircuit) {
			ctx.SetRequestItem(ADMIN_SHORT_CIRCUIT, adminShortCircuit);
		}

		public static bool GetAdminShortCircuit(this HttpContext ctx) {
			return ctx.GetRequestItem<bool>(ADMIN_SHORT_CIRCUIT);
		}

		public static void SetIsRadialAdmin(this HttpContext ctx, bool isRadialAdmin) {
			ctx.SetRequestItem(IS_RADIAL_ADMIN, isRadialAdmin);
		}

		public static bool IsRadialAdmin(this HttpContext ctx) {
			return ctx.GetRequestItem<bool>(IS_RADIAL_ADMIN);
		}
	}
}
