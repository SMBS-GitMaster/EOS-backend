using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.GraphQL.Models.Mutations;
using System;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    Task<GraphQLResponseBase> EditMeetingInstanceAttendee(EditMeetingInstanceAttendeeModel meetingInstanceAttendeeModel);

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    public async Task<GraphQLResponseBase> EditMeetingInstanceAttendee(EditMeetingInstanceAttendeeModel model)
    {
      try
      {
        await L10Accessor.UpdateMeetingAttendee(caller, model.MeetingInstanceId, model.UserId, model.Rating, model.NotesText);
        return GraphQLResponseBase.Successfully();
      }
      catch (Exception ex)
      {
        return GraphQLResponseBase.Error(ex);
      }
    }

  }

}