using Microsoft.AspNetCore.Mvc.Filters;
using RadialReview.Exceptions;
using RadialReview.Middleware.Request.HttpContextExtensions;
using RadialReview.Middleware.Request.HttpContextExtensions.Permissions;
using RadialReview.Models;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using System;
using System.Threading.Tasks;
using static RadialReview.Models.UserOrganizationModel;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class LoginMetadataFilter : IAsyncActionFilter {
		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			var httpCtx = context.HttpContext;
			var isRadialAdmin = false;
			if (httpCtx.IsLoggedIn()) {
				UserOrganizationModel oneUser = null;
				try {
					oneUser = httpCtx.GetUser();
					if (oneUser == null) {
						throw new NoUserOrganizationException();
					}
					var actualUserModel = httpCtx.GetUserModel();
					var adminShortCircuit = httpCtx.GetAdminShortCircuit();
					isRadialAdmin = oneUser.IsRadialAdmin || actualUserModel.IsRadialAdmin;
					oneUser._IsRadialAdmin = isRadialAdmin;
					SetupPermissionsOverride(oneUser, actualUserModel, isRadialAdmin, adminShortCircuit);
					

					if (!isRadialAdmin) {
						SaveLastLoginMetadata(oneUser);
					}
				} catch (OrganizationIdException) {
				} catch (NoUserOrganizationException) {
				}
			}
			httpCtx.SetIsRadialAdmin(isRadialAdmin);
			await next();
		}

		private static void SaveLastLoginMetadata(UserOrganizationModel oneUser) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var lu = s.Get<UserLookup>(oneUser.Cache.Id);
					lu.LastLogin = DateTime.UtcNow;
					s.Update(lu);

					var ol = s.QueryOver<OrganizationLookup>().Where(x => x.OrgId == oneUser.Organization.Id).Take(1).SingleOrDefault();

					if (ol == null) {
						ol = new OrganizationLookup() {
							OrgId = oneUser.Organization.Id,
							CreateTime = oneUser.Organization.CreationTime
						};
					}

					ol.LastUserLogin = oneUser.Id;
					ol.LastUserLoginTime = lu.LastLogin.Value;
					s.SaveOrUpdate(ol);

					tx.Commit();
					s.Flush();
				}
			}
		}

		private void SetupPermissionsOverride(UserOrganizationModel oneUser, UserModel actualUserModel, bool isRadialAdmin, bool adminShortCircuit) {

			var asc = new AdminShortCircuit() { IsRadialAdmin = isRadialAdmin };

			oneUser._PermissionsOverrides = oneUser._PermissionsOverrides ?? new PermissionsOverrides();
			oneUser._PermissionsOverrides.Admin = asc;

			if (actualUserModel != null) {
        asc.ActualUserId = actualUserModel.Id;
       
        if (oneUser.User != null) {
          asc.EmulatedUserId = oneUser.User.Id;
          asc.IsMocking = oneUser.User.Id != actualUserModel.Id;
				}
			}

			if (adminShortCircuit) {
				asc.AllowAdminWithoutAudit = true;
			}

		}






	
	}
}
