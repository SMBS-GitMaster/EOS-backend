namespace RadialReview.GraphQL.Models
{
    public class MeetingRatingModel
    {

        #region Base Properties

        public long Id { get; set; }
        public int Version { get; set; }
        public string LastUpdatedBy { get; set; }
        public double DateCreated { get; set; }
        public double DateLastModified { get; set; }

        #endregion

        #region Properties

        public decimal? Rating { get; set; }

        public string NotesId { get; set; }

        public UserQueryModel Attendee { get; set; }

        #endregion

        public static class Associations
        {
            public enum User3 
            {
                Attendee
            }
        }
    }
}