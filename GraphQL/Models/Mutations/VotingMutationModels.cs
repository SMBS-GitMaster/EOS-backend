using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Models.Mutations {
  public class VotingUpdateStateModel {
    public string IssueVotingState { get; set; }
    public long MeetingId { get; set; }
  }

  public class StartStarVotingModel {
    public string IssueVotingType { get; set; }
    public string IssueVotingTime { get; set; }
  }

  public class SubmitIssueStarVotesModel {
    public long IssueId { get; set; }
    public double NumberOfVotes { get; set; }
  }
}
