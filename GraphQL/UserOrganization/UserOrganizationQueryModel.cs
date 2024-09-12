using System;

namespace RadialReview.GraphQL.Models
{

    public class UserOrganizationQueryModel
    {

        public long UserOrganizationId { get; set; }
        public long OrganizationId { get; set; }

        public string UserEmail { get; set; }

        [Obsolete("do we need to expose this?")]
        public long UserId { get; set; }

        public bool IsSuperAdmin{ get; set; }

        public bool IsImplementer{ get; set; }

        public int NumViewedNewFeatures { get; set; }

    }

}