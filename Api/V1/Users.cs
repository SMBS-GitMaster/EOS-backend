using System.Collections.Generic;
using System.Linq;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Angular.Accountability;

using Microsoft.AspNetCore.Mvc;
namespace RadialReview.Api.V1 {
	[Route("api/v1")]
    public class Users_Controller : BaseApiController
    {
        public Users_Controller(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
        {
        }

        [Route("users/{USER_ID:long}")]
        [HttpGet]
        public AngularUser GetUser(long USER_ID)
        {
            return AngularUser.CreateUser(UserAccessor.GetUserOrganization(GetUser(), USER_ID, false, false));
        }

        [Route("users/mine")]
        [HttpGet]
        public AngularUser GetMineUser()
        {
            return GetUser(GetUser().Id);
        }

        /// <summary>
        /// Get direct reports for a particular user
        /// </summary>
        /// <returns></returns>
        [Route("users/{userId:long}/directreports")]
        [HttpGet]
        public IEnumerable<AngularUser> GetDirectReports(long userId)
        {
            return UserAccessor.GetDirectSubordinates(GetUser(), userId).Select(x => AngularUser.CreateUser(x));
        }

        /// <summary>
        /// Get my direct reports
        /// </summary>
        /// <returns></returns>
        [Route("users/minedirectreports")]
        [HttpGet]
        public IEnumerable<AngularUser> GetMineDirectReports()
        {
            return GetDirectReports(GetUser().Id);
        }

        /// <summary>
        /// Get all viewable users
        /// </summary>
        /// <returns></returns>
        [Route("users/mineviewable")]
        [HttpGet]
        public IEnumerable<AngularUser> GetMineViewable()
        {
            return OrganizationAccessor.GetAngularUsers(GetUser(), GetUser().Organization.Id);
        }

        //[GET] /users/{userid}/seats/
        [Route("users/{userId:long}/seats")]
        [HttpGet]
        public IEnumerable<AngularAccountabilityNode> GetSeats(long userId)
        {
            return AccountabilityAccessor.GetNodesForUser(GetUser(), userId).Select(x => new AngularAccountabilityNode(x));
        }
    }
}