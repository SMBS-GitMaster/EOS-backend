using HotChocolate.Types;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.GraphQL.Types.Mutations;
using RadialReview.Repositories;

namespace RadialReview.GraphQL.Types.Mutations
{
  public partial class EditMeetingInstanceAttendeeMutationType : InputObjectType<EditMeetingInstanceAttendeeModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<EditMeetingInstanceAttendeeModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }
}

namespace RadialReview.GraphQL
{
  public partial class MutationType
  {
    public void AddMeetingInstanceAttendeeMutations(IObjectTypeDescriptor descriptor)
    {

      descriptor
        .Field("EditMeetingInstanceAttendee")
        .Argument("input", a => a.Type<NonNullType<EditMeetingInstanceAttendeeMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditMeetingInstanceAttendee(ctx.ArgumentValue<EditMeetingInstanceAttendeeModel>("input")));
    }
  }
}