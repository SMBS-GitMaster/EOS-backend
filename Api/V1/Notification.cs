using Microsoft.AspNetCore.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Notifications;
using RadialReview.Models.Notifications;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Api.V1
{
    [Route("api/v1")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class NotificationController : BaseApiController
    {
        public NotificationController(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
        {
        }

        /// <summary>
        /// Get a specific notification
        /// </summary>
        /// <param name = "NOTIFICATION_ID">Notification ID</param>
        /// <returns>The headline</returns>
         //[GET/POST/DELETE] /headline/{id}
        [Route("notification/{NOTIFICATION_ID:long}")]
        [HttpGet]
        public AngularAppNotification GetNotification(long NOTIFICATION_ID)
        {
            return new AngularAppNotification(NotificationAccessor.GetNotification(GetUser(), NOTIFICATION_ID));
        }

        public class NotificationStatusModel
        {
            /// <summary>
            /// Notification status
            /// </summary>
            [Required]
            public NotificationStatus status { get; set; }
        }

        /// <summary>
        /// Set notification status
        /// </summary>
        /// <param name = "NOTIFICATION_ID">Notification ID</param>
        /// <returns>The headline</returns>
        [Route("notification/{NOTIFICATION_ID:long}")]
        [HttpPost]
        public async Task Status(long NOTIFICATION_ID, [FromBody] NotificationStatusModel model)
        {
            await NotificationAccessor.SetNotificationStatus(GetUser(), NOTIFICATION_ID, model.status);
        }

        /// <summary>
        /// Get a list of notifications
        /// </summary>
        /// <returns></returns>
        [Route("notification/list")]
        [HttpGet]
        public async Task<List<AngularAppNotification>> List(bool seen = false)
        {
            return (await NotificationAccessor.GetNotificationsForUser(GetUser(), GetUser().Id, seen ? DateTime.MinValue : (DateTime? )null)).Select(x => new AngularAppNotification(x)).ToList();
        }
    }
}