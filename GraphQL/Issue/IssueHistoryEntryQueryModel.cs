using System;
using System.Collections.Generic;

namespace RadialReview.GraphQL.Models
{
    public class IssueHistoryEntryQueryModel
    {

        #region Base Properties

        public long Id { get; set; }
        public int Version { get; set; }
        public string LastUpdatedBy { get; set; }
        public double DateCreated { get; set; }
        public double DateLateModified { get; set; }
        public double LateUpdatedClientTimestemp { get; set; }
        public string Type { get { return "issueHistoryEntry"; } }

        #endregion

        #region Properties

        public RadialReview.Models.Issues.IssueHistoryEventType EventType { get; set; }

        public DateTime? ValidFrom {get; set;}

        public DateTime? ValidUntil {get; set;}

        public long IssueId {get; set;}

        public long MeetingId {get; set;}

        #endregion

        public static class Associations 
        {
            public enum Meeting2 
            {
                Meeting
            }
        }

    }
}