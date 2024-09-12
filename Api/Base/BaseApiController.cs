using System.Linq;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RadialReview.Identity;
using Microsoft.AspNetCore.Authorization;

namespace RadialReview.Api {


	[Authorize]
	public class BaseApiController : ControllerBase, IApiController {
		private UserOrganizationModel _CurrentUser = null;
		private UserOrganizationModel MockUser = null;

    protected readonly RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext _dbContext;
    protected readonly RedLockNet.IDistributedLockFactory _redLockFactory;
    protected BaseApiController(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory)
    {
      _dbContext = dbContext;
      _redLockFactory = redLockFactory;
    }

		private UserOrganizationModel ForceGetUser(ISession s, string userId) {
			var user = s.Get<UserModel>(userId);
			if (user.IsRadialAdmin)
				_CurrentUser = s.Get<UserOrganizationModel>(user.CurrentRole);
			else {
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
					if (_CurrentUser == null)
						throw new NoUserOrganizationException("No user exists.");
				} else {
					_CurrentUser = found;
				}
			}

			return _CurrentUser;
		}

		public UserOrganizationModel CurrentUser {
			get {
				return GetUser();
			}
		}

		protected UserOrganizationModel GetUser() //long? organizationId, Boolean full = false)
		{
			if (MockUser != null)
				return MockUser;
			if (_CurrentUser != null)
				return _CurrentUser;
			var userId = User.GetUserId();
			if (userId == null)
				throw new LoginException("Not logged in.", null);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return ForceGetUser(s, userId);
				}
			}
		}

		protected UserOrganizationModel GetUser(ISession s) //long? organizationId, Boolean full = false)
		{
			if (MockUser != null)
				return MockUser;
			if (_CurrentUser != null)
				return _CurrentUser;
			var userId = User.GetUserId();
			if (userId == null)
				throw new LoginException("Not logged in.", null);
			return ForceGetUser(s, userId);
		}
	}

	/*[SwaggerName(Name = "Title")]*/
	public class TitleModel {
		/// <summary>
		/// Title
		/// </summary>
		[Required]
		public string title { get; set; }
	}

	/*[SwaggerName(Name = "Name")]*/
	public class NameModel {
		/// <summary>
		/// Name
		/// </summary>
		[Required]
		public string name { get; set; }
	}
}