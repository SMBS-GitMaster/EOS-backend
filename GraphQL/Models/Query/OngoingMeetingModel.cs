using System;

namespace RadialReview.GraphQL.Models
{
    public class OngoingMeetingModel
    {

        public string Type { get { return "ongoingMeeting"; } }

        #region Properties

        public DateTime? MeetingStartTime { get; set; }
        public string Name { get; set; }

         public long Id { get; set; }

    #endregion

  }

}