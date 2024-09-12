namespace RadialReview.GraphQL.Models
{
    public class PageNameModel
    {

        #region Base Properties

        public long Id { get; set; }
        public int Version { get; set; }
        public string LastUpdatedBy { get; set; }
        public double DateCreated { get; set; }
        public double DateLateModified { get; set; }
        public double LateUpdatedClientTimestemp { get; set; }
        public string Type { get { return "pageName"; } }

    #endregion

    #region Properties
        public double TimeLastStarted { get; set; }
        public double TimeLastSaved { get; set; }
        public double? TimePerviouslySpentS { get; set; }
        public double TimeLastPaused { get; set; }
        public double TimeSpentPausedS { get; set; }

        #endregion
    }
}