using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using RadialReview.Accessors;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.Identity;
using RadialReview.Models;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using static RadialReview.Models.UserOrganizationModel;
using ISession = NHibernate.ISession;

namespace RadialReview.Middleware.Request.HttpContextExtensions {
	public static partial class HttpContextItems {

		private static HttpContextItemKey GenerateUserModelKey(string userGuid, bool styles) {
			return new HttpContextItemKey("UserModel", "styles=" + styles, "userguid=" + userGuid);
		}
		private static HttpContextItemKey GenerateUserOrganizationModelKey(long userOrganizationId) {
			return new HttpContextItemKey("UserOrganizationModel", "uoid=" + userOrganizationId);
		}

		public static string GetUserId(this HttpContext ctx) {
			return ctx.User.GetUserId();
		}

		public static UserModel GetUserModel(this HttpContext ctx, bool styles = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
                  if (ctx.User.Identity.AuthenticationType == "Identity.Application")
                  {
                    var userGuid = ctx.User.GetUserId();
                    return ctx.GetUserModel(s, userGuid, styles);
                  } else
                  {
                    var userEmail = ctx.User.GetEmail();
                    return ctx.GetUserModelByEmail(s, userEmail, styles);
                  }
				}
			}
		}

		public static UserModel GetUserModel(this HttpContext ctx, ISession s, string unsafeUserGuid, bool styles = false) {
			return ctx.GetOrCreateRequestItem(GenerateUserModelKey(unsafeUserGuid, styles), x => {

				var user = UserAccessor.Unsafe.GetUserModelById(s, unsafeUserGuid);

				if (styles) {
					user._StylesSettings = s.Get<UserStyleSettings>(unsafeUserGuid);
					//Optimistically store this one as well.
					var keyWithStyleFalse = GenerateUserModelKey(unsafeUserGuid, false);
					if (!x.ContainsRequestItem(keyWithStyleFalse)) {
						x.SetRequestItem(keyWithStyleFalse, user);
					}
				}
				return user;
			});
		}

        public static UserModel GetUserModelByEmail(this HttpContext ctx, ISession s, string email, bool styles = false)
        {
          return ctx.GetOrCreateRequestItem(GenerateUserModelKey(email, styles), x => {

            var user = UserAccessor.Unsafe.GetUserByEmail(s, email);

            if (styles)
            {
              user._StylesSettings = s.Get<UserStyleSettings>(user.Id);
              //Optimistically store this one as well.
              var keyWithStyleFalse = GenerateUserModelKey(user.Id, false);
              if (!x.ContainsRequestItem(keyWithStyleFalse))
              {
                x.SetRequestItem(keyWithStyleFalse, user);
              }
            }
            return user;
          });
        }

    public static void InjectPermissionOverrides(this HttpContext ctx, PermissionsOverrides overrides) {
			Unsafe.PopulateUserData(ctx, Unsafe.GetUser(ctx), overrides);
		}

		public static UserOrganizationModel GetUser(this HttpContext ctx) {
			return Unsafe.PopulateUserData(ctx, Unsafe.GetUser(ctx), null);
		}

		public static void ClearUserOrganizationFromCache(this HttpContext ctx) {
			ctx.ClearRequestItem(GenerateUserOrganizationModelKey(-1), false);
		}


		public static bool IsLoggedIn(this HttpContext ctx) {
			if (ctx.User == null || ctx.User.Identity == null) {
				return false;
			}
			return ctx.User.GetUserId() != null;
		}

		private static class Unsafe {

			public static HttpContextItemKey CALLER = new HttpContextItemKey("Caller");

			public static UserOrganizationModel GetUser(HttpContext ctx) {
				return ctx.GetOrCreateRequestItem(CALLER, x => {
					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
                            IEnumerable<UserAccessor.Unsafe.UserOrgIdAndGuid> possibleUserOrgIds;
                            UserModel userModel;
                            if(ctx.User.Identity.AuthenticationType == "Identity.Application")
                            {
                              var userGuid = ctx.User.GetUserId();
                              //Call first, it is a "future" request.
                              possibleUserOrgIds = UserAccessor.Unsafe.GetUserOrganizationIdsForUser(s, userGuid);
                              //Call second to resolve both requests.
                              userModel = x.GetUserModel(s, userGuid, false);
                            } else
                            {
                              possibleUserOrgIds = UserAccessor.Unsafe.GetUserOrganizationIdsForUserByEmail(s, ctx.User.GetEmail());
                              userModel = UserAccessor.Unsafe.GetUserByEmail(s, ctx.User.GetEmail());  
                            }

							var currentRoleId = userModel.GetCurrentRole();

							if (!possibleUserOrgIds.Any(x => x.UserOrgId == currentRoleId) && !userModel.IsRadialAdmin) {
								//Clear out invalid roles
								currentRoleId = null;
							}

							if (currentRoleId != null) {
								//Found role.. get UserOrg.
								var userId = userModel.Id;
								return _getUserOrganizationFromRole(x, s, userId, currentRoleId.Value);
							}

							if (possibleUserOrgIds.Count() == 0) {
								//No roles exist
								throw new NoUserOrganizationException();
							} else if (possibleUserOrgIds.Count() == 1) {
								//Exactly one user exists.
								var uoGuidAndId = possibleUserOrgIds.First();
								if (userModel != null) {
									if (!s.Contains(userModel)) {
										s.Evict(userModel);
										userModel = s.Get<UserModel>(uoGuidAndId.UserGuid);
									}
									userModel.CurrentRole = uoGuidAndId.UserOrgId;
									s.Update(userModel);
									tx.Commit();
									s.Flush();
								}
								return _getUserOrganizationFromRole(x, s, uoGuidAndId.UserGuid, uoGuidAndId.UserOrgId);
							} else {
								throw new OrganizationIdException(x.Request.GetEncodedPathAndQuery());
							}
						}
					}
				});
			}
			public static UserOrganizationModel PopulateUserData(HttpContext ctx, UserOrganizationModel user, PermissionsOverrides overrides) {

				if (user != null && ctx.Request != null) {
					if (overrides != null) {
						user._PermissionsOverrides = overrides;
					}

					user._IsRadialAdmin = user.IsRadialAdmin;
					user._ClientTimestamp = _getOrNull(ctx.Request.Query["_clientTimestamp"]).TryParseLong();
					user._ClientOffset = _getOrNull(ctx.Request.Query["_tz"]).TryParseInt();
					user._ClientRequestId = _getOrNull(ctx.Request.Query["_rid"]);
					user._ConnectionId = _getOrNull(ctx.Request.Query["connectionId"]);
					if (user._ClientTimestamp != null && user._ClientOffset == null) {
						var diff = (int)(Math.Round((user._ClientTimestamp.Value.ToDateTime() - DateTime.UtcNow).TotalMinutes / 30.0) * 30.0);
						user._ClientOffset = diff; // Thread.SetData(Thread.GetNamedDataSlot("timeOffset"), diff);
					}

					HookData.SetData("ConnectionId", _getOrNull(ctx.Request.Query["connectionId"]));
					HookData.SetData("ClientTimestamp", user._ClientTimestamp);
					HookData.SetData("ClientTimezone", user._ClientOffset);
					HookData.SetData("ClientRequestId", user._ClientRequestId);
				}

				return user;
			}

			private static UserOrganizationModel _postProcessUser(ISession s, UserOrganizationModel userOrg) {
				//Warm up lookup cache
				var _ = userOrg.Cache.Name;
				return userOrg;
			}

			private static UserOrganizationModel _getUserOrganizationFromRole(HttpContext ctx, ISession s, string userId, long userOrganizationId) {
				return ctx.GetOrCreateRequestItem(GenerateUserOrganizationModelKey(userOrganizationId), x => {
					var found = UserAccessor.Unsafe.GetUserOrganizationForUserIdAndRole(s, userId, userOrganizationId);
					return _postProcessUser(s, found);
				});
			}

			private static string _getOrNull(StringValues sv) {
				if (StringValues.IsNullOrEmpty(sv))
					return null;
				return sv.ToString();
			}
		}
	}
}
