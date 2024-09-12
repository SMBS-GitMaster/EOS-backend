using System.Threading.Tasks;
using RadialReview.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.AspNetCore.Authentication;
using RadialReview.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using RadialReview.Hubs;

namespace RadialReview.Controllers {
	public partial class BaseController : Controller {
    protected async Task ExecuteSignOut() {

			var _signInManager = (SignInManager<UserModel>)HttpContext.RequestServices.GetService(typeof(SignInManager<UserModel>));
      var userManager = (UserManager<UserModel>)HttpContext.RequestServices.GetService(typeof(UserManager<UserModel>));
      var hubContext = (IHubContext<LogoutHub>)HttpContext.RequestServices.GetService(typeof(IHubContext<LogoutHub>));
      
      var user = await userManager.GetUserAsync(HttpContext.User);
      if (user != null)
      {
        var browserSessionId = HttpContext.Request.Headers["BrowserSessionId"].ToString(); 
        await hubContext.Clients.User(user.Id.ToString()).SendAsync("LogoutNotification", browserSessionId);

        // update SecurityStamp to invalidate the cookie
        await userManager.UpdateSecurityStampAsync(user);
      }
      await HttpContext.SignOutAsync();
      await _signInManager.SignOutAsync();
		}

		protected bool IsLoggedIn() {
			if (User == null || User.Identity == null) {
				return false;
			}
			return User.IsLoggedIn();
		}


		protected void AllowAdminsWithoutAudit() {
			try {
				var user = GetUser();
				if (user != null && user._PermissionsOverrides != null) {
					user._PermissionsOverrides.Admin.AllowAdminWithoutAudit = true;
				}
			} catch (Exception) {
			}

		}


	}
}
