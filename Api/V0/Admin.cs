using Microsoft.AspNetCore.Mvc;
using RadialReview.Accessors;
using RadialReview.Exceptions;

#region DO NOT EDIT, V0
namespace RadialReview.Api.V0
{
    [Route("api/v0")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AdminController : BaseApiController
    {
        public AdminController(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
        {
        }

        // GET: api/Scores/5
        [HttpGet]
        [Route("app/stats")]
        public ApplicationAccessor.AppStat Stats()
        {
            if (!(GetUser().IsRadialAdmin || GetUser().User.IsRadialAdmin))
                throw new PermissionsException();
            return ApplicationAccessor.Stats();
        }
    }
}
#endregion
