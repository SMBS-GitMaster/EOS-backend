using System;
using System.Collections.Generic;

namespace RadialReview.GraphQL.Models
{
    public class MeetingSummaryModel
    {

        #region Base Properties

        public long Id { get; set; }
        public int Version { get; set; }
        public string LastUpdatedBy { get; set; }
        public double DateCreated { get; set; }
        public double DateLateModified { get; set; }
        public double LateUpdatedClientTimestemp { get; set; }
        public string Type { get { return "meetingSummary"; } }

        #endregion

        #region Properties

        public string RatingPrivacy { get; set; }

        public string SendTo { get; set; }

        public bool IncludeMeetingNotes { get; set; }

        public bool ArchiveCompletedTodos { get; set; }

        public bool ArchiveHeadlines { get; set; }

        public bool ArchiveCompletedIssues { get; set; }

        public List<MeetingRatingModel> Rating { get; set; }

        public List<MeetingNoteQueryModel> Notes { get; set; }

        #endregion
    }
}