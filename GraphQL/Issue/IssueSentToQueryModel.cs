using System;
using System.Linq;
using System.Collections.Generic;

namespace RadialReview.GraphQL.Models
{
    public class IssueSentToMeetingDTO
    {

        #region Base Properties

        public long Id { get; set; }
        public int Version { get; set; }
        public string LastUpdatedBy { get; set; }
        public double DateCreated { get; set; }
        public double DateLastModified { get; set; }

        #endregion

        #region Properties

        public bool Archived { get; set; }

        public double? ArchivedTimestamp { get; set; }

        public long MeetingId { get; set; }

        public long IssueId { get; set; }

        public IssueQueryModel Issue { get; set; }

        public UserQueryModel Assignee { get; set; }

        #endregion
    }

    public class IssueSentToQueryModel
    {

        #region Base Properties

        public long Id { get; set; }
        public int Version { get; set; }
        public string LastUpdatedBy { get; set; }
        public double DateCreated { get; set; }
        public double DateLastModified { get; set; }
        public string Type { get { return "issueSentTo"; } }

        #endregion

        #region Properties

        public bool Archived { get; set; }

        public double? ArchivedTimestamp { get; set; }

        public IssueQueryModel Issue { get; set; }

        public IQueryable<IssueHistoryEntryQueryModel> IssueHistoryEntries { get; set; }

        public UserQueryModel Assignee { get; set; }

        #endregion

        public static class Collections
        {
            public enum IssueHistoryEntry1
            {
                IssueHistoryEntries
            }
        }

        public static class Associations 
        {
            public enum User11
            {
                User
            }

            public enum Issue1 
            {
                Issue
            }

            public enum Meeting1 
            {
                Meeting
            }
        }

    }
}