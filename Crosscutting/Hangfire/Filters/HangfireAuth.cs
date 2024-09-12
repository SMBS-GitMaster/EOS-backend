using Hangfire.Dashboard;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Identity;
using RadialReview.Utilities;
using System;

namespace RadialReview.Crosscutting.Hangfire.Filters {
	public class HangfireAuth : IDashboardAuthorizationFilter {

		public bool Authorize(DashboardContext context) {
			// Allow all authenticated users to see the Dashboard
			// (potentially dangerous).
			try {
				var userId = context.GetHttpContext().User.GetUserId();
				var user = UserAccessor.Unsafe.GetUserModelById(userId);
				if (user != null) {
					return user.IsRadialAdmin;
				}
			} catch (Exception e) {
				int a = 0;
			} finally {
				var res = HibernateSession.CloseCurrentSession();
			}

			return false;
		}
	}
}