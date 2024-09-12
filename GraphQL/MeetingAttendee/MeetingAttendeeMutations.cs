using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Models.Mutations;
using System;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    Task<GraphQLResponseBase> MeetingAddAttendee(long meetingId, long userId);

    Task<GraphQLResponseBase> MeetingEditAttendee(MeetingEditAttendeeModel model);

    Task<GraphQLResponseBase> MeetingRemoveAttendee(long meetingId, long userId);

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    public async Task<GraphQLResponseBase> MeetingAddAttendee(long meetingId, long userId)
    {
      try
      {
        await L10Accessor.AddAttendee(caller, meetingId, userId);
        return GraphQLResponseBase.Successfully();
      }
      catch (Exception ex)
      {
        return GraphQLResponseBase.Error(ex);
      }
    }

    public async Task<GraphQLResponseBase> MeetingEditAttendee(MeetingEditAttendeeModel model)
    {
      try
      {
        await L10Accessor.EditAttendee(caller, model.MeetingId, model.MeetingAttendee, model.IsPresent, model.HasSubmittedVotes, model.IsUsingV3);
        if (model.permissions != null)
          PermissionsAccessor.EditL10RecurrencePermItemByUserId(caller, model.MeetingId, model.MeetingAttendee, model.permissions.View, model.permissions.Edit, model.permissions.Admin);

        return GraphQLResponseBase.Successfully();
      }
      catch (Exception ex)
      {

        return new GraphQLResponseBase(false, ex.Message);
      }
    }

    public async Task<GraphQLResponseBase> MeetingRemoveAttendee(long meetingId, long userId)
    {
      try
      {
        var detachTime = DateTime.UtcNow;

        await L10Accessor.RemoveAttendee(caller, meetingId, userId, detachTime);
        return GraphQLResponseBase.Successfully();
      }
      catch (Exception ex)
      {
        return GraphQLResponseBase.Error(ex);
      }
    }

  }
}
