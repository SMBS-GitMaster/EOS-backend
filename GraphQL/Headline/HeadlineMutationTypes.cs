using HotChocolate.Subscriptions;
using HotChocolate.Types;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.GraphQL.Types;
using RadialReview.Repositories;


namespace RadialReview.Core.GraphQL.Types.Mutations {


  public partial class HeadlineCreateMutationType : InputObjectType<HeadlineCreateModel> {
    protected override void Configure(IInputObjectTypeDescriptor<HeadlineCreateModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class HeadlineEditMutationType : InputObjectType<HeadlineEditModel> {
    protected override void Configure(IInputObjectTypeDescriptor<HeadlineEditModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class CopyHeadlineToMeetingsMutationType : InputObjectType<CopyHeadlineToMeetingsModel> {
    protected override void Configure(IInputObjectTypeDescriptor<CopyHeadlineToMeetingsModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

}

namespace RadialReview.GraphQL {
    public partial class MutationType {
    public void AddHeadlineMutations(IObjectTypeDescriptor descriptor) {


      descriptor
        .Field("CreateHeadline")
        .Argument("input", a => a.Type<NonNullType<HeadlineCreateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateHeadline(ctx.ArgumentValue<HeadlineCreateModel>("input")));

      descriptor
        .Field("EditHeadline")
        .Argument("input", a => a.Type<NonNullType<HeadlineEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditHeadline(ctx.ArgumentValue<HeadlineEditModel>("input")));


      descriptor
        .Field("CopyHeadlineToMeetings")
        .Argument("input", a => a.Type<NonNullType<CopyHeadlineToMeetingsMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CopyHeadlineToMeetings(ctx.ArgumentValue<CopyHeadlineToMeetingsModel>("input")));
    }
  }
}
