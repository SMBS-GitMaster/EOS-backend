using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using log4net;
using RadialReview.Identity;
using RadialReview.Utilities.NHibernate;
using System.Text;

namespace RadialReview.Hubs {
	public class BaseHub : Hub {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		protected UserAccessor _UserAccessor = new UserAccessor();
		public static String REGISTERED_KEY = "BaseHubRegistered_";

		private UserOrganizationModel _CurrentUser = null;
		private string _CurrentUserOrganizationId = null;
		private UserOrganizationModel ForceGetUser_Unsafe(IOuterSession s, string userId) {
			var user = s.Get<UserModel>(userId);
			if (user.IsRadialAdmin) {
				_CurrentUser = s.Get<UserOrganizationModel>(user.CurrentRole);
				if (Config.IsTest()) {
					_CurrentUser._IsTestAdmin = true;
				}

			} else {
				if (user.CurrentRole == 0) {
					if (user.UserOrganizationIds != null && user.UserOrganizationIds.Count() == 1) {
						user.CurrentRole = user.UserOrganizationIds[0];
						s.Update(user);
					} else {
						throw new OrganizationIdException();
					}
				}

				var found = s.Get<UserOrganizationModel>(user.CurrentRole);
				if (found.DeleteTime != null || found.User.Id == userId) {
					//Expensive
					var avail = user.UserOrganization.ToListAlive();
					_CurrentUser = avail.FirstOrDefault(x => x.Id == user.CurrentRole);
					if (_CurrentUser == null)
						_CurrentUser = avail.FirstOrDefault();
					if (_CurrentUser == null) {
						try {
							log.Info($@"No user exists: ({user.CurrentRole}) ({found.User.Id}) ({found.DeleteTime})");
						} catch (Exception) {
						}
						throw new NoUserOrganizationException("No user exists.");
					}
				} else {
					_CurrentUser = found;
				}


			}
			return _CurrentUser;
		}

		protected string GetUserId() {
			return Context.User.GetUserId();
		}

		//private UserOrganizationModel GetUser(){
		//	if (_CurrentUser != null)
		//		return _CurrentUser;
		//	var userId = GetUserId();
		//	if (userId==null)
		//		throw new LoginException("Not logged in.");

		//	using (var s = HibernateSession.CreateIsolatedSession()){
		//		using (var tx = s.BeginTransaction()){
		//			return ForceGetUser(s, userId);
		//		}
		//	}
		//}		

		protected UserOrganizationModel GetUser(IOuterSession s) {
			if (_CurrentUser != null)
				return _CurrentUser;
			var userId = Context.User.GetUserId();
			if (userId == null) {
				var sb = new StringBuilder();
				try {
					sb.Append("Not logged in to hub.");
					AddToSb(sb, "connectionId", () => Context.ConnectionId);
					AddToSb(sb, "userId", () => Context.User.GetUserId());
					AddToSb(sb, "userIdentifier", () => Context.UserIdentifier);
					AddToSb(sb, "userName", () => Context.User.GetUserName());
				} catch (Exception e) {
				}
				log.Info(sb.ToString());
				throw new LoginException("Not logged in.", null);
			}
			return ForceGetUser_Unsafe(s, userId);
		}

		private static void AddToSb(StringBuilder sb, string k, Func<string> v) {
			sb.Append('[');
			sb.Append(k);
			sb.Append('=');
			try {
				sb.Append(v());
			} catch (Exception e) {
				sb.Append('?');
			}
			sb.Append(']');
		}
	}
}