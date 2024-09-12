using System.Collections.Generic;

namespace RadialReview.GraphQL.Models
{
    public class BloomLookupModel
    {

        #region Base Properties

        public long Id { get; set; }
        public int Version { get; set; }
        public string LastUpdatedBy { get; set; }
        public double DateCreated { get; set; }
        public double DateLateModified { get; set; }
        public double LateUpdatedClientTimestemp { get; set; }
        public string Type { get { return "bloomLookupNode"; } }

        #endregion

        #region Properties

        public string ClassicCheckinTitle { get; set; }

        public List<string> IceBreakers { get; set; }

        public string TipOfTheWeek { get; set; }

        #endregion

    }
}