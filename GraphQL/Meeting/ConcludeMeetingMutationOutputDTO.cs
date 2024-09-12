using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Models
{
  public record ConcludeMeetingMutationOutputDTO(decimal AverageMeetingRating, double? MeetingDurationInSeconds, int IssuesSolvedCount, decimal TodosCompletedPercentage, string FeedbackStyle);
}
