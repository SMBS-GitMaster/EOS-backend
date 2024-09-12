using RadialReview.Core.GraphQL;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Models;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {


    Task<IdModel> SubmitFeedback(FeedbackSubmitModel feedbackSubmitModel);
    [Obsolete("slow, use metadata endpoint instead", DebugConst.COMPILE_TIME_ERROR_ON_SLOW_QUERY)]

    Task<IncrementNumViewedNewFeaturesOutput> IncrementNumViewedNewFeatures(IncrementNumViewedNewFeaturesInput input, CancellationToken cancellationToken);
    Task<IdModel> UpdateVotingState(VotingUpdateStateModel votingUpdateStateModel);

    Task<IdModel> StartStarVoting(StartStarVotingModel startStarVotingModel);
    Task<IdModel> SubmitIssueStarVotes(SubmitIssueStarVotesModel submitIssueStarVotesModel);


    Task<long> GetCallerId();

    Task<GraphQLResponse<bool>> CreateIssueSentTo(IssueCreateSentToModel sentto);

    GraphQLResponse<bool> IframeEmbedCheck(string url);


  }
}