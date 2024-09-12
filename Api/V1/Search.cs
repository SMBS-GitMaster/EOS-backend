using Microsoft.AspNetCore.Mvc;
using RadialReview.Accessors;
using System.Collections.Generic;

namespace RadialReview.Api.V1 {
	[Route("api/v1")]
    public class SearchController : BaseApiController
    {
        public SearchController(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
        {
        }

        /// <summary>
        /// Search for users, positions, or teams by name
        /// </summary>
        /// <param name = "term">Search term</param>
        /// <returns>A list of results</returns>
         //[GET] /search?term={term}
        [Route("search/all")]
        [HttpGet]
        public IEnumerable<SearchResult> Search(string term)
        {
            return SearchAccessor.SearchOrganizationRGM(GetUser(), GetUser().Organization.Id, term);
        }

        /// <summary>
        /// Search for users
        /// </summary>
        /// <param name = "term">Search term</param>
        /// <returns>A list of results</returns>
         //[GET] /search/user? term = { term }
        [Route("search/user")]
        [HttpGet]
        public IEnumerable<SearchResult> SearchUser(string term)
        {
            return SearchAccessor.SearchOrganizationUsers(GetUser(), GetUser().Organization.Id, term, true);
        }
    }
}