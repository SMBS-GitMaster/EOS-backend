using RadialReview.Exceptions;
using System.Threading.Tasks;

namespace RadialReview.Middleware.ExceptionHandlers {
	public class HandleAdminSetRoleException : IExceptionHandler {
		public bool CanProcess(ExceptionHandlerContext ex) {
			return ex.Exception is AdminSetRoleException;
		}

		public async Task ProcessException(ExceptionHandlerProcessor handlerContext) {
			var ex = (AdminSetRoleException)handlerContext.Exception;
			var redirectUrl = ex.RedirectUrl;

			//skip if filterContext.IsChildAction

			if (ex.AccessLevel == Models.Admin.AdminAccessLevel.View) {
				await handlerContext.Redirect("/Account/AdminSetRole/" + ex.RequestedRoleId + "?returnUrl=" + redirectUrl);
			}else if (ex.AccessLevel == Models.Admin.AdminAccessLevel.SetAs) {
				await handlerContext.Redirect("/Account/SetAsUser/" + ex.RequestedRoleId + "?returnUrl=" + redirectUrl);
			}
		}
	}
}
