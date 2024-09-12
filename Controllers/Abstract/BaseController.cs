using log4net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace RadialReview.Controllers {
	public partial class BaseController : Controller {
		protected static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    protected readonly RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext _dbContext;
    protected readonly RedLockNet.IDistributedLockFactory _redLockFactory;

    protected BaseController(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory)
    {
      _dbContext = dbContext;
      _redLockFactory = redLockFactory;
    }

		//SEE PARTIALS FOR BASECONTROLLER METHODS.

	}
}
