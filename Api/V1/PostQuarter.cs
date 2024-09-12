using System;
using RadialReview.Accessors;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RadialReview.Api.V1 {
	[Route("api/v1")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PostQuarterController : BaseApiController
    {
        public PostQuarterController(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
        {
        }

        public class CreateNewQuarterModel
        {
            /// <summary>
            /// New Quarter title
            /// </summary>
            [Required]
            public string name { get; set; }

            /// <summary>
            /// New Quarter date
            /// </summary>
            [Required]
            public DateTime quarterenddate { get; set; }

            /// <summary>
            /// Meeting ID
            /// </summary>
            [Required]
            public long meetingId { get; set; }
        }

        /// <summary>
        /// Create a new post quarter
        /// </summary>
        /// <returns>HTTP response 200</returns>
         //[Obsolete("Not for public use")]
        [Route("postquarter/create")]
        [HttpPost]
        public async Task<long> Create([FromBody] CreateNewQuarterModel body)
        {
            var postQuarter = new Models.PostQuarter.PostQuarterModel();
            postQuarter.L10RecurrenceId = body.meetingId;
            postQuarter.QuarterEndDate = body.quarterenddate.Date;
            postQuarter.Name = body.name;
            var newQuarter = await PostQuarterAccessor.CreatePostQuarter(GetUser(), postQuarter);
            return newQuarter.Id;
        }
    }
}