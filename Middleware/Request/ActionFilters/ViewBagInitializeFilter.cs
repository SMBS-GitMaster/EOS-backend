using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RadialReview.Accessors;
using RadialReview.Controllers;
using RadialReview.Middleware.Request.HttpContextExtensions;
using RadialReview.Middleware.Request.HttpContextExtensions.Navbar;
using RadialReview.Middleware.Request.HttpContextExtensions.Prefetch;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using System.Reflection;
using System.Threading.Tasks;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class ViewBagInitializeFilter : IAsyncActionFilter {
		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			if (context.Controller is Controller) {
				var ctrl = context.Controller as Controller;
				var prefetch = context.HttpContext.GetPrefetchData();
				var viewbag = ctrl.ViewBag;

				viewbag.IsRadialAdmin = false;
				viewbag.IsLocal = Config.IsLocal();
				viewbag.AppVersion = GetAppVersion();
				viewbag.KnowledgeBaseUrl = prefetch.KnowledgeBaseUrl;
				viewbag.Settings = SettingsAccessor.GenerateViewSettings(null, "", false, false, prefetch, TermsCollection.DEFAULT);
				viewbag.NavBar = context.HttpContext.GetNavBar();
				if (context.Controller is BaseController) {
					viewbag.HasBaseController = true;
				}

				if (context.HttpContext.IsLoggedIn()) {
					viewbag.SupportContactCode = "";
          viewbag.v3ShowFeatures = false;
          viewbag.UserName = MessageStrings.User;
					viewbag.UserImage = "/i/placeholder";
					viewbag.UserInitials = "";
					viewbag.Email = "";
					viewbag.UserColor = 0;
					viewbag.IsManager = false;
					viewbag.ShowL10 = false;
					viewbag.ShowReview = false;
					viewbag.ShowSurvey = false;
					viewbag.ShowPeople = false;
					viewbag.ShowWhale = false;
					viewbag.Organizations = 0;
					viewbag.Hints = true;
					viewbag.ManagingOrganization = false;
					viewbag.Organization = null;
					viewbag.UserId = 0L;
					viewbag.ConsoleLog = false;
					viewbag.LimitFiveState = true;
					viewbag.ShowAC = false;
					viewbag.ShowCoreProcess = false;
					viewbag.EvalOnly = false;
					viewbag.PrimaryVTO = null;
				}
			}

			await next();
		}


		protected string GetAppVersion() {
			var version = Assembly.GetExecutingAssembly().GetName().Version;
			return version.ToString();
		}

	}
}
