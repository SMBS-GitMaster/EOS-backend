using HotChocolate.Subscriptions;
using HotChocolate.Types;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.Repositories;
using System;
using RadialReview.Core.GraphQL.Common.DTO;

namespace RadialReview.Core.GraphQL.Types.Mutations {

  public partial class ConcludeMeetingMutationType : InputObjectType<ConcludeMeetingModel> {
    protected override void Configure(IInputObjectTypeDescriptor<ConcludeMeetingModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class StartMeetingMutationType : InputObjectType<StartMeetingModel> {
    protected override void Configure(IInputObjectTypeDescriptor<StartMeetingModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class RestartMeetingMutationType : InputObjectType<RestartMeetingModel> {
    protected override void Configure(IInputObjectTypeDescriptor<RestartMeetingModel> descriptor) {
      base.Configure(descriptor);
    }
  }
  public partial class SetMeetingPageMutationType : InputObjectType<SetMeetingPageModel> {
    protected override void Configure(IInputObjectTypeDescriptor<SetMeetingPageModel> descriptor) {
      base.Configure(descriptor);
    }
  }
  public partial class SetCurrentMeetingPageMutationType : InputObjectType<SetCurrentMeetingPageModel> {
    protected override void Configure(IInputObjectTypeDescriptor<SetCurrentMeetingPageModel> descriptor) {
      base.Configure(descriptor);
    }
  }
  public partial class RateMeetingModelMutationType : InputObjectType<RateMeetingModel> {
    protected override void Configure(IInputObjectTypeDescriptor<RateMeetingModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class CreateMeetingPageMutationType : InputObjectType<CreateMeetingPageModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<CreateMeetingPageModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public class SetTangentAlertInput
  {
    public long RecurrenceId { get; set; }
    public double? TangentAlertTimestamp {get; set; }
  }

}

namespace RadialReview.GraphQL {
  public partial class MutationType {

    public static void AddMeetingActionMutations(IObjectTypeDescriptor descriptor) {

      descriptor
        .Field("startMeeting")
        .Argument("input", a => a.Type<NonNullType<StartMeetingMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => {
          return await ctx.Service<IRadialReviewRepository>().StartMeeting(ctx.ArgumentValue<StartMeetingModel>("input"));
        });
      ;

      descriptor
      .Field("restartMeeting")
      .Argument("input", a => a.Type<NonNullType<RestartMeetingMutationType>>())
      .Authorize()
      .Resolve(async (ctx, cancellationToken) => {
        return await ctx.Service<IRadialReviewRepository>().RestartMeeting(ctx.ArgumentValue<RestartMeetingModel>("input"));
      })
      ;

      descriptor
      .Field("concludeMeeting")
      .Argument("input", a => a.Type<NonNullType<ConcludeMeetingMutationType>>())
      .Authorize()
      .Resolve(async (ctx, cancellationToken) => {
        return await ctx.Service<IRadialReviewRepository>().ConcludeMeeting(ctx.ArgumentValue<ConcludeMeetingModel>("input"));
      })
      ;

      descriptor
        .Field("SetTangentAlert")
        .Argument("input", a => a.Type<NonNullType<InputObjectType<SetTangentAlertInput>>>())
        .Resolve(async (ctx, cancellationToken) => {
          try
          {
            await ctx.Service<IRadialReviewRepository>().SetTangentAlert(ctx.ArgumentValue<SetTangentAlertInput>("input"), cancellationToken);
            return new GraphQLResponseBase(success: true, "Tangent Alert signalled");
          }
          catch(Exception ex)
          {
            return GraphQLResponseBase.Error(ex);
          }
        })
        .Authorize();

      descriptor
      .Field("setCurrentMeetingPage")
      .Argument("input", a => a.Type<NonNullType<SetCurrentMeetingPageMutationType>>())
      .Authorize()
      .Resolve(async (ctx, cancellationToken) => {
        var arg = ctx.ArgumentValue<SetCurrentMeetingPageModel>("input");
        return await ctx.Service<IRadialReviewRepository>().SetMeetingPage(arg.MeetingId,arg.NewPageId,arg.CurrentPageId,arg.MeetingPageStartTime);
      })
      ;

      //Standardized format
      //descriptor
      //.Field("setCurrentMeetingPage")
      //.Argument("meetingId", a => a.Type<NonNullType<LongType>>())
      //.Argument("pageName", a => a.Type<NonNullType<StringType>>())
      //.Argument("meetingPageStartTime", a => a.Type<NonNullType<LongType>>())
      //.Authorize()
      //.Resolve(async (ctx, cancellationToken) => {
      //  var output = await ctx.Service<IRadialReviewRepository>().SetMeetingPage(
      //  ctx.ArgumentValue<long>("meetingId"),
      //  ctx.ArgumentValue<string>("pageName"),
      //  ctx.ArgumentValue<long>("meetingPageStartTime")
      //  );
      //  throw new Exception("TODO TopicEventSender must be inside the accessor");
      //  await ctx.Service<ITopicEventSender>().SendAsync("setMeetingPage", output);
      //  return output;
      //})
      //;


      descriptor
        .Field("SetMeetingPage")
        .Argument("input", a => a.Type<NonNullType<SetMeetingPageMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => {
          return await ctx.Service<IRadialReviewRepository>().SetMeetingPage(ctx.ArgumentValue<SetMeetingPageModel>("input"));
        });

      descriptor
        .Field("CreateMeetingPage")
        .Argument("input", a => a.Type<NonNullType<CreateMeetingPageMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => {
          return await ctx.Service<IRadialReviewRepository>().CreateMeetingPage(ctx.ArgumentValue<CreateMeetingPageModel>("input"));
        });


      descriptor
        .Field("RateMeeting")
        .Argument("input", a => a.Type<NonNullType<RateMeetingModelMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => {
          var args = ctx.ArgumentValue<RateMeetingModel>("input");
          return await ctx.Service<IRadialReviewRepository>().RateMeeting(args.MeetingId, args.Rating, args.Notes);
        });


      //Standardized format
      //descriptor
      //  .Field("rateMeeting")
      //  .Argument("meetingId", a => a.Type<NonNullType<LongType>>())
      //  .Argument("rating", a => a.Type<NonNullType<IntType>>())
      //  .Argument("notes", a => a.Type<StringType>())
      //  .Authorize()
      //  .Resolve(async (ctx, cancellationToken) => {
      //    var input = await ctx.Service<IRadialReviewRepository>().RateMeeting(ctx.ArgumentValue<long>("meetingId"), ctx.ArgumentValue<int>("rating"), ctx.ArgumentValue<string>("notes"));
      //    throw new Exception("TODO TopicEventSender must be inside the accessor or V1 page updates will not propagate to V3");

      //    await ctx.Service<ITopicEventSender>().SendAsync("ratingMeeting", input);
      //    return input;
      //  });

    }
  }
}