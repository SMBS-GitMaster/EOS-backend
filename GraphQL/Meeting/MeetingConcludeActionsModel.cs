using RadialReview.Core.GraphQL.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models
{
  public class MeetingConcludeActionsModel
  {

    #region Properties

    public gqlSendEmailSummaryTo SendEmailSummaryTo { get; set; }

    public bool IncludeMeetingNotesInEmailSummary { get; set; }

    public bool ArchiveCompletedTodos { get; set; }

    public bool ArchiveHeadlines { get; set; }

    public bool DisplayMeetingRatings { get; set; }

    public gqlFeedbackStyle FeedbackStyle { get; set; }

    #endregion

    public static MeetingConcludeActionsModel Default()
    {
      return new()
      {
        SendEmailSummaryTo = gqlSendEmailSummaryTo.NONE,
        FeedbackStyle = gqlFeedbackStyle.ALL_PARTICIPANTS,
      };
    }

  }
}
