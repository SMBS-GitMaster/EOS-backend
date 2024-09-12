using HotChocolate.Types;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;

namespace RadialReview.GraphQL {

  public partial class MeetingCreateMutationType : InputObjectType<MeetingCreateModel> {
    protected override void Configure(IInputObjectTypeDescriptor<MeetingCreateModel> descriptor) {
      base.Configure(descriptor);
    }
  }
  public partial class MeetingEditMutationType : InputObjectType<MeetingEditModel> {
    protected override void Configure(IInputObjectTypeDescriptor<MeetingEditModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class MeetingEditConcludeActionsMutationType : InputObjectType<MeetingEditConcludeActionsModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MeetingEditConcludeActionsModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class MeetingEditMeetingInstanceMutationType : InputObjectType<MeetingEditMeetingInstanceModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MeetingEditMeetingInstanceModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }


  public partial class MeetingEditLastViewedTimestampMutationType : InputObjectType<MeetingEditLastViewedTimestampModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MeetingEditLastViewedTimestampModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }
}

namespace RadialReview.GraphQL {
  public partial class MutationType {
    public void AddMeetingMutations(IObjectTypeDescriptor descriptor) {

      descriptor
        .Field("CreateMeeting")
        .Argument("input", a => a.Type<NonNullType<MeetingCreateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateMeeting(ctx.ArgumentValue<MeetingCreateModel>("input")));

      descriptor
        .Field("EditMeeting")
        .Argument("input", a => a.Type<NonNullType<MeetingEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditMeeting(ctx.ArgumentValue<MeetingEditModel>("input")));

      descriptor
        .Field("EditMeetingConcludeActions")
        .Argument("input", a => a.Type<NonNullType<MeetingEditConcludeActionsMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditMeetingConcludeActions(ctx.ArgumentValue<MeetingEditConcludeActionsModel>("input")));

      AddMeetingAttendeeMutationTypes(descriptor);

      descriptor
        .Field("EditMeetingInstance")
        .Argument("input", a => a.Type<NonNullType<MeetingEditMeetingInstanceMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditMeetingInstance(ctx.ArgumentValue<MeetingEditMeetingInstanceModel>("input")));

      descriptor
       .Field("EditMeetingLastViewedTimestamp")
       .Argument("input", a => a.Type<NonNullType<MeetingEditLastViewedTimestampMutationType>>())
       .Authorize()
       .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditMeetingLastViewedTimestamp(ctx.ArgumentValue<MeetingEditLastViewedTimestampModel>("input")));

      //Standardized
      //descriptor
      // .Field("addAttendee")
      // .Argument("meetingId", a => a.Type<NonNullType<LongType>>())
      // .Argument("userId", a => a.Type<NonNullType<LongType>>())
      // .Authorize()
      // .Resolve(async (ctx, cancellationToken) => {
      //   return await ctx.Service<IRadialReviewRepository>().MeetingAddAttendee(
      //     ctx.ArgumentValue<long>("meetingId"),
      //     ctx.ArgumentValue<long>("userId")
      //   );
      // });

    }
  }
}
