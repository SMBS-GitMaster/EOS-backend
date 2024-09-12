using System;

namespace RadialReview.GraphQL.Models
{
    public record StartMeetingMutationOutputDTO(long MeetingId, DateTime StartTime);
}
