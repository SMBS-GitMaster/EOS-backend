using Microsoft.AspNetCore.Mvc;

namespace RadialReview.Api.test {

    namespace RadialReview.Api.InvalidNamespace {
        [Route("api/v0")]
        public class SanityCheckUnsetNamespaceController : BaseApiController {
            public SanityCheckUnsetNamespaceController(global::RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
            {
            }

            /// <summary>
            /// I should only appear when running locally, I belong in the [FAILED TO MAP NAMESPACE TO VERSION] document (which also only appears locally)
            /// Other endpoints that are mapped incorrectly will be in this document too. Please fix their namespaces before releasing.
            /// </summary>
            /// <returns></returns>
            [HttpGet]
            [Route("sanity/check")]
            public bool Sanity() {
                return false;
            }
        }
    }
}
