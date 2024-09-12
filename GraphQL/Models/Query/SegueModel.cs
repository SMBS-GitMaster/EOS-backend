namespace RadialReview.GraphQL.Models
{
    public class SegueModel
    {

        #region Base Properties

        public long Id { get; set; }
        public int Version { get; set; }
        public string LastUpdatedBy { get; set; }
        public double DateCreated { get; set; }
        public double DateLateModified { get; set; }
        public double LateUpdatedClientTimestemp { get; set; }
        public string Type { get { return "segue"; } }

        #endregion

        #region Properties

        public string SegueType { get; set; }

        public string Text { get; set; }

        public string WeeklyTip { get; set; }

        public bool IsAttendanceVisible { get; set; }

        #endregion

    }
}