using HotChocolate.Types;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;
using RadialReview.GraphQL.Models;

namespace RadialReview.GraphQL
{

  public partial class MeetingPageEditOrderMutationType : InputObjectType<EditMeetingPageOrder>
  {
    protected override void Configure(IInputObjectTypeDescriptor<EditMeetingPageOrder> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class RemoveMeetingPageMutationType : InputObjectType<RemoveMeetingPageModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<RemoveMeetingPageModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class EditMeetingPageMutationType : InputObjectType<EditMeetingPageModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<EditMeetingPageModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class MutationType
  {
    public void AddMeetingPageMutations(IObjectTypeDescriptor descriptor)
    {

      descriptor
        .Field("EditMeetingPageOrder")
        .Argument("input", a => a.Type<NonNullType<MeetingPageEditOrderMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditMeetingPageOrder(ctx.ArgumentValue<EditMeetingPageOrder>("input")));

      descriptor
        .Field("RemoveMeetingPageFromMeeting")
        .Argument("input", a => a.Type<NonNullType<RemoveMeetingPageMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().RemoveMeetingPage(ctx.ArgumentValue<RemoveMeetingPageModel>("input")));

      descriptor
         .Field("EditMeetingPage")
         .Argument("input", a => a.Type<NonNullType<EditMeetingPageMutationType>>())
         .Authorize()
         .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditMeetingPage(ctx.ArgumentValue<EditMeetingPageModel>("input")));

    }
  }
}
