using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.ViewModels;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.Positions;
using RadialReview.Models.Angular.Accountability;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Team;
using Microsoft.AspNetCore.Mvc;

#region DO NOT EDIT, V0
namespace RadialReview.Api.V0 {
	[Route("api/v0")]
	public class UsersController : BaseApiController {
    public UsersController(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
    {
    }

		// GET: api/Scores/5
		[HttpGet]
		[Route("users/{id:long}")]
		public UserOrganizationModel.DataContract Get(long id) {
			return UserAccessor.GetUserOrganization(GetUser(), id, false, false).GetUserDataContract();
		}

		[HttpGet]
		[Route("users/{username}")]
		public UserOrganizationModel.DataContract Get(string username) {
			var self = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					UserOrganizationModel found = null;
					try {
						found = UserAccessor.Unsafe.GetUserOrganizationsForUser(s, username).
							FirstOrDefault(x => x.Organization.Id == self.Organization.Id);
					} catch (LoginException) {
					}

					if (found == null)
						throw new HttpException(HttpStatusCode.BadRequest, "Unknown user");
					PermissionsUtility.Create(s, self).ViewUserOrganization(found.Id, false);
					return found.GetUserDataContract();
				}
			}
		}

		[HttpGet]
		[Route("users/organization/{id?}")]
		public IEnumerable<UserOrganizationModel.DataContract> GetOrganizationUsers(long? id = null) {
			return OrganizationAccessor.GetOrganizationMembers(GetUser(), id ?? GetUser().Organization.Id, false, false).Select(x => x.GetUserDataContract());
		}

		[HttpGet]
		[Route("users/managing")]
		public IEnumerable<UserOrganizationModel.DataContract> GetUsersManaged() {
			return DeepAccessor.Users.GetSubordinatesAndSelfModels(GetUser(), GetUser().Id).Select(x => x.GetUserDataContract());
		}

		//--
		[HttpPost]
		[Route("users/")]
		public async Task<AngularUser> CreateUser([FromBody] string firstName, [FromBody] string lastName, [FromBody] string email, [FromBody] long? managerNodeId = null, [FromBody] bool? SendEmail = null) {
			//var outParam = new UserOrganizationModel();
			if (!SendEmail.HasValue) {
				SendEmail = GetUser().Organization.SendEmailImmediately;
			}

			var model = new CreateUserOrganizationViewModel() { FirstName = firstName, LastName = lastName, Email = email, OrgId = GetUser().Organization.Id, SendEmail = SendEmail.Value };
			var result = await JoinOrganizationAccessor.CreateUserUnderManager(GetUser(), model);
			return AngularUser.CreateUser(result.User);
		}

		//[GET/DELETE] /users/{userId}
		[HttpGet]
		[Route("users/{userId}")]
		public AngularUser GetUser(long userId) {
			var user = UserAccessor.GetUserOrganization(GetUser(), userId, false, false);
			return AngularUser.CreateUser(user);
		}

		[HttpDelete]
		[Route("users/{userId}")]
		public async Task DeleteUsers(long userId) {
			await UserAccessor.RemoveUser(GetUser(), userId, DateTime.UtcNow);
		}

		//[GET] /users/{userid}/roles/
		[HttpGet]
		[Route("users/{userId}/roles")]
		public IEnumerable<AngularRole> GetUserRoles(long userId) {
			return AccountabilityAccessor.GetRolesForUser(GetUser(), userId).Select(x => new AngularRole(x));
		}

		// [GET] /users/{userid}/positions/
		/// <summary>
		/// Get the positions for a user
		/// </summary>
		/// <param name = "userId">The user's id</param>
		/// <param name = "includeUnnamed">Include unnamed positions. Default: false</param>
		/// <returns></returns>
		[HttpGet]
		[Route("users/{userId}/positions")]
		public IEnumerable<AngularPosition> GetUserPositions(long userId, bool includeUnnamed = false) {
			return AccountabilityAccessor.GetPositionsForUser(GetUser(), userId, includeUnnamed);
		}

		//[GET/PUT] /users/{userid}/directreports/
		[HttpGet]
		[Route("users/{userId}/directreports")]
		public IEnumerable<AngularUser> GetDirectReports(long userId) // wrap AngularUser
		{
			return UserAccessor.GetDirectSubordinates(GetUser(), userId).Select(x => AngularUser.CreateUser(x));
		}

		//[GET] /users/{userid}/supervisors/
		[HttpGet]
		[Route("users/{userId}/supervisors")]
		public IEnumerable<AngularUser> GetSupervisors(long userId) {
			return UserAccessor.GetManagers(GetUser(), userId).Select(x => AngularUser.CreateUser(x));
		}

		//[GET] /users/{userid}/seats/
		[HttpGet]
		[Route("users/{userId}/seats")]
		public IEnumerable<AngularAccountabilityNode> GetSeats(long userId) {
			return AccountabilityAccessor.GetNodesForUser(GetUser(), userId).Select(x => new AngularAccountabilityNode(x));
		}

		//[GET] /user/mine/teams
		[HttpGet]
		[Route("users/mine/teams")]
		public IEnumerable<AngularTeam> GetMineTeam() {
			//throw new NotImplementedException("Obfuscate the TeamDurationModel");
			return TeamAccessor.GetUsersTeams(GetUser(), GetUser().Id).Select(x => new AngularTeam(x.Team));
		}

		//[GET] /user/{userId}/teams
		[HttpGet]
		[Route("users/{userId}/teams")]
		public IEnumerable<AngularTeam> GetUserTeams(long userId) {
			//throw new NotImplementedException("Obfuscate the TeamDurationModel");
			return TeamAccessor.GetUsersTeams(GetUser(), userId).Select(x => new AngularTeam(x.Team));
		}
	}
}
#endregion
