using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Accessors;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Angular.Positions;
using RadialReview.Models.Angular.Roles;
using Microsoft.AspNetCore.Mvc;

namespace RadialReview.Api.V1 {
	/// <summary>
	/// Seats are boxes on the organizational chart. A user can occupy more than one seat on the organizational chart.
	/// </summary>
	[Route("api/v1")]
	public class SeatsController : BaseApiController {
    public SeatsController(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
    {
    }

		/// <summary>
		/// Add a user below a seat
		/// </summary>
		/// <param name = "SEAT_ID">Seat ID</param>
		/// <param name = "USER_ID">User ID</param>
		/// <returns></returns>
		[Route("seats/{SEAT_ID:long}/directreport/{USER_ID:long}")]
		[HttpPost]
		public async Task<AngularAccountabilityNode> AttachDirectReport(long SEAT_ID, long USER_ID) {
			return new AngularAccountabilityNode(await AccountabilityAccessor.AppendNode(GetUser(), SEAT_ID, null, new List<long> { USER_ID }));
		}

		/// <summary>
		/// Get a particular seat
		/// </summary>
		/// <param name = "SEAT_ID">Seat ID</param>
		/// <returns></returns>
		// [GET/POST/(DELETE?)] /seats/{seatId}
		[Route("seats/{SEAT_ID:long}")]
		[HttpGet]
		public AngularAccountabilityNode GetSeat(long SEAT_ID) {
			return new AngularAccountabilityNode(AccountabilityAccessor.GetNodeById(GetUser(), SEAT_ID));
		}

		/// <summary>
		/// Delete a seat from the organizational chart
		/// </summary>
		/// <param name = "SEAT_ID">Seat ID</param>
		[Route("seats/{SEAT_ID:long}")]
		[HttpDelete]
		public async Task RemoveSeat(long SEAT_ID) {
			await AccountabilityAccessor.RemoveNode(GetUser(), SEAT_ID);
		}

		/// <summary>
		/// Get the position attached to a seat
		/// </summary>
		/// <param name = "SEAT_ID">Seat ID</param>
		/// <returns></returns>
		//[GET/PUT/DELETE] /seats/{SEAT_ID}/position
		[Route("seats/{SEAT_ID:long}/position")]
		[HttpGet]
		public AngularPosition GetPosition(long SEAT_ID) {
			var node = AccountabilityAccessor.GetNodeById(GetUser(), SEAT_ID);
			if (!string.IsNullOrEmpty(node.AccountabilityRolesGroup.PositionName)) {
				return new AngularPosition(-1, node.AccountabilityRolesGroup.NotNull(x => x.PositionName));
			} else {
				throw new HttpException(404, "Seat does not contain a position.");
			}
		}

		/// <summary>
		/// Set the position for a seat
		/// </summary>
		/// <param name = "SEAT_ID">Seat ID</param>
		/// <param name = "POSITION_ID">Position ID</param>
		[Route("seats/{SEAT_ID:long}/position/{POSITION_ID:long}")]
		[HttpPost]
		[Obsolete("Deprecated. Use [PUT] seats/{SEAT_ID}/position/")]
		public void AttachPosition(long SEAT_ID, long POSITION_ID) {
			throw new NotImplementedException("This method is deprecated. Use [PUT] seats/{SEAT_ID}/position/ instead.");
			//AccountabilityAccessor.SetPosition(GetUser(), SEAT_ID, POSITION_ID);
		}

		/// <summary>
		/// Set the position for a seat
		/// </summary>
		/// <param name = "SEAT_ID">Seat ID</param>
		/// <param name = "title">Position Title</param>
		[Route("seats/{SEAT_ID:long}/position/")]
		[HttpPut]
		public async Task SetPosition(long SEAT_ID, [FromBody] TitleModel title) {
			await AccountabilityAccessor.SetPosition(GetUser(), SEAT_ID, title.NotNull(x => x.title));
			//AccountabilityAccessor.SetPosition(GetUser(), SEAT_ID, POSITION_ID);
		}

		/// <summary>
		/// Remove position for a seat
		/// </summary>
		/// <param name = "SEAT_ID">Seat ID</param>
		[Route("seats/{SEAT_ID:long}/position")]
		[HttpDelete]
		public async Task RemovePosition(long SEAT_ID) {
			await AccountabilityAccessor.SetPosition(GetUser(), SEAT_ID, null);
		}

		/// <summary>
		/// Get the user for a seat
		/// </summary>
		/// <param name = "SEAT_ID">Seat ID</param>
		/// <returns></returns>
		[Route("seats/{SEAT_ID:long}/user")]
		[HttpGet]
		[Todo]
		[Obsolete("Deprcated. Use seats/{SEAT_ID}/users instead")] //<< This is correct "userS
		public AngularUser GetSeatUser(long SEAT_ID) // Angular
		{
			var getUser = AccountabilityAccessor.GetNodeById(GetUser(), SEAT_ID).GetUsers(null).FirstOrDefault();
			return AngularUser.CreateUser(getUser);
		}

		/// <summary>
		/// Get the users for a seat
		/// </summary>
		/// <param name = "SEAT_ID">Seat ID</param>
		/// <returns></returns>
		[Route("seats/{SEAT_ID:long}/users")]
		[HttpGet]
		[Todo]
		public IEnumerable<AngularUser> GetSeatUsers(long SEAT_ID) // Angular
		{
			var getUsers = AccountabilityAccessor.GetNodeById(GetUser(), SEAT_ID).GetUsers(null);
			return getUsers.Select(x => AngularUser.CreateUser(x));
		}

		/// <summary>
		/// Set a user for a seat
		/// </summary>
		/// <param name = "SEAT_ID">Seat ID</param>
		/// <param name = "USER_ID">User ID</param>
		[Route("seats/{SEAT_ID:long}/user/{USER_ID}")]
		[HttpPost]
		public async Task AttachUser(long SEAT_ID, long USER_ID) {
			await AccountabilityAccessor.AddUserToNode(GetUser(), SEAT_ID, USER_ID);
		}

		/// <summary>
		/// Remove user from a seat
		/// </summary>
		/// <param name = "SEAT_ID">Seat ID</param>
		/// <param name = "USER_ID">User ID</param>
		[Route("seats/{SEAT_ID:long}/user")]
		[HttpDelete]
		public async Task DetachUser(long SEAT_ID, long? USER_ID = null) {
			if (USER_ID == null) {
				await AccountabilityAccessor.RemoveAllUsersFromNode(GetUser(), SEAT_ID);
			} else {
				await AccountabilityAccessor.RemoveUserFromNode(GetUser(), SEAT_ID, USER_ID.Value); // null userId for detaching
			}
		}

		/// <summary>
		/// Get seats for a user
		/// </summary>
		/// <param name = "USER_ID">User ID</param>
		/// <returns></returns>
		[Route("seats/user/{USER_ID:long}")]
		[HttpGet]
		public IEnumerable<AngularAccountabilityNode> GetSeatsForUser(long USER_ID) {
			return AccountabilityAccessor.GetNodesForUser(GetUser(), USER_ID).Select(x => new AngularAccountabilityNode(x));
		}

		/// <summary>
		/// Get your seats
		/// </summary>
		/// <returns></returns>
		[Route("seats/user/mine")]
		[HttpGet]
		public IEnumerable<AngularAccountabilityNode> GetSeatsForMe() {
			return AccountabilityAccessor.GetNodesForUser(GetUser(), GetUser().Id).Select(x => new AngularAccountabilityNode(x));
		}

		/// <summary>
		/// Get roles from a seat
		/// </summary>
		/// <param name = "SEAT_ID">Seat ID</param>
		[Route("seats/{SEAT_ID:long}/roles")]
		[HttpGet]
		public IEnumerable<AngularRole> GetRolesForSeat(long SEAT_ID) {
			return AccountabilityAccessor.GetRolesForSeat(GetUser(), SEAT_ID);
		}

		/// <summary>
		/// Create a role from a seat
		/// </summary>
		/// <param name = "SEAT_ID">Seat ID</param>
		/// <param name = "body">name</param>
		[Route("seats/{SEAT_ID:long}/role")]
		[HttpPut]
		[Todo]
		public async Task AddRole(long SEAT_ID, NameModel body) {
			await AccountabilityAccessor.AddRole(GetUser(), SEAT_ID, body.name);
		}
	}

	[Route("api/v1")]
	public class RoleController : BaseApiController {
    public RoleController(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
    {
    }

		/// <summary>
		/// Get a particular role
		/// </summary>
		/// <param name = "ROLE_ID"></param>
		/// <returns>The specified role</returns>
		//[GET/POST/DELETE] /roles/{id}
		[Route("role/{ROLE_ID:long}")]
		[HttpGet]
		public AngularRole GetRoles(long ROLE_ID) // Angular
		{
			return new AngularRole(AccountabilityAccessor.GetRole(GetUser(), ROLE_ID));
		}

		/// <summary>
		/// Update a seat's role
		/// </summary>
		/// <param name = "ROLE_ID"></param>
		/// <param name = "title">Updated role</param>
		/// <returns></returns>
		[Route("role/{ROLE_ID:long}")]
		[HttpPut]
		public async Task UpdateRoles(long ROLE_ID, [FromBody] TitleModel title) {
			await AccountabilityAccessor.UpdateRole(GetUser(), ROLE_ID, title.title);
		}

		/// <summary>
		/// Remove a role from a seat
		/// </summary>
		/// <param name = "ROLE_ID">Role ID</param>
		/// <returns></returns>
		[Route("role/{ROLE_ID:long}")]
		[HttpDelete]
		public async Task RemoveRole(long ROLE_ID) {
			await AccountabilityAccessor.RemoveRole(GetUser(), ROLE_ID);
		}

		/// <summary>
		/// Restore a role from a position
		/// </summary>
		/// <param name = "ROLE_ID">Role ID</param>
		/// <returns></returns>
		[Route("role/{ROLE_ID:long}/restore")]
		[HttpPatch]
		public async Task RestoreRole(long ROLE_ID) {
			await AccountabilityAccessor.RestoreRole(GetUser(), ROLE_ID);
		}
	}

	[Route("api/v1")]
	public class PositionController : BaseApiController {
    public PositionController(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
    {
    }

		/// <summary>
		/// List all your positions at the organization
		/// </summary>
		/// <returns>A list of your positions</returns>
		//[GET] /positions/mine
		[Route("positions/mine")]
		[HttpGet]
		public AngularPosition[] GetMinePosition() {
			//20210130 to-finish
			throw new NotImplementedException();
			//return PositionAccessor.GetPositionModelForUser(GetUser(), GetUser().Id).Select(x => new AngularPosition(x)).ToArray();
		}

		/// <summary>
		/// Get a list of roles for a particular position
		/// </summary>
		/// <param name = "POSITION_ID"></param>
		//[GET/PUT] /positions/{id}/roles/
		[Route("positions/{POSITION_ID:long}/roles")]
		[HttpGet]
		[Obsolete("Deprecated. Use [GET] seats/{SEAT_ID}/roles/ instead.")]
		public IEnumerable<AngularRole> GetPositionRoles(long POSITION_ID) {
			// do it later.
			throw new NotImplementedException("Deprecated. Use [GET] seats/{SEAT_ID}/roles/ instead.");
			//return PositionAccessor.GetPositionRoles(GetUser(), POSITION_ID).Select(x => new AngularRole(x));
		}

		/// <summary>
		/// Create a role for a position
		/// </summary>
		/// <param name = "POSITION_ID">Position ID</param>
		/// <param name = "body">Role title</param>
		/// <returns>The created role</returns>
		[Route("positions/{POSITION_ID:long}/roles")]
		[Obsolete("Deprecated. Use [PUT] seats/{SEAT_ID}/role/ instead.")]
		[HttpPost]
		public async Task<AngularRole> AddPositionRoles(long POSITION_ID, [FromBody] TitleModel body) {
			throw new NotImplementedException("Deprecated. Use [PUT] seats/{SEAT_ID}/role/ instead.");
			//return new AngularRole(await AccountabilityAccessor.AddRole(GetUser(), new Attach(AttachType.Position, POSITION_ID), body.title));
		}

		/// <summary>
		/// Create a new position
		/// </summary>
		/// <returns></returns>
		//[PUT] /positions/
		[Route("positions/create")]
		[HttpPost]
		[Obsolete("Deprecated. Use [PUT] seats/{SEAT_ID}/position/ instead.")]
		public async Task<AngularPosition> CreatePosition([FromBody] TitleModel body) {
			throw new NotImplementedException("This method is deprecated. Use [PUT] seats/{SEAT_ID}/position/ instead.");
			////need to discuss?
			//OrganizationAccessor _accessor = new OrganizationAccessor();
			//var position = await _accessor.EditOrganizationPosition(GetUser(), 0, GetUser().Organization.Id, body.title);
			//return new AngularPosition(position);
		}

		/// <summary>
		/// Get a particular position
		/// </summary>
		/// <param name = "POSITION_ID">Position ID</param>
		/// <returns>The specified position</returns>
		//[GET/POST] /positions/{id}
		[Route("positions/{POSITION_ID:long}")]
		[HttpGet]
		[Obsolete("Deprecated.")]
		public AngularPosition GetPositions(long POSITION_ID) {
			throw new NotImplementedException("This method is deprecated.");
			//return new AngularPosition(new OrganizationAccessor().GetOrganizationPosition(GetUser(), POSITION_ID));
		}

		/// <summary>
		/// Update a position
		/// </summary>
		/// <param name = "POSITION_ID">Position ID</param>
		/// <param name = "body">Position name</param>
		/// <returns></returns>
		[Route("positions/{POSITION_ID:long}")]
		[Obsolete("Deprecated. Use [PUT] seats/{SEAT_ID}/position/ instead.")]
		[HttpPut]
		public async Task UpdatePositions(long POSITION_ID, [FromBody] TitleModel body) {
			throw new NotImplementedException("Deprecated. Use [PUT] seats/{SEAT_ID}/position/ instead.");
			//OrganizationAccessor _accessor = new OrganizationAccessor();
			//var position = await _accessor.EditOrganizationPosition(GetUser(), POSITION_ID, GetUser().Organization.Id, body.title);
			//new AngularPosition(position);
		}
	}
}