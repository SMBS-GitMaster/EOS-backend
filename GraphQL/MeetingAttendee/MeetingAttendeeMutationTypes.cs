using HotChocolate.Types;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;

namespace RadialReview.GraphQL
{

  public partial class MeetingAddAttendeeMutationType : InputObjectType<MeetingAddAttendeeModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MeetingAddAttendeeModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class MeetingEditAttendeeMutationType : InputObjectType<MeetingEditAttendeeModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MeetingEditAttendeeModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class MeetingRemoveAttendeeMutationType : InputObjectType<MeetingRemoveAttendeeModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MeetingRemoveAttendeeModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class MeetingAttendeeIsPresentType : InputObjectType<MeetingAttendeeIsPresentModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<MeetingAttendeeIsPresentModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class MutationType
  {

    public void AddMeetingAttendeeMutationTypes(IObjectTypeDescriptor descriptor)
    {

      descriptor
        .Field("AddAttendee")
        .Argument("input", a => a.Type<NonNullType<MeetingAddAttendeeMutationType>>())
        .Authorize()
         .Resolve(async (ctx, cancellationToken) => {
           var args = ctx.ArgumentValue<MeetingAddAttendeeModel>("input");
           return await ctx.Service<IRadialReviewRepository>().MeetingAddAttendee(args.MeetingId, args.UserId);
         });

      descriptor
       .Field("EditMeetingAttendee")
       .Argument("input", a => a.Type<NonNullType<MeetingEditAttendeeMutationType>>())
       .Authorize()
       .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().MeetingEditAttendee(ctx.ArgumentValue<MeetingEditAttendeeModel>("input")));

      descriptor
        .Field("RemoveMeetingAttendee")
        .Argument("input", a => a.Type<NonNullType<MeetingRemoveAttendeeMutationType>>())
        .Authorize()
         .Resolve(async (ctx, cancellationToken) => {
           var args = ctx.ArgumentValue<MeetingRemoveAttendeeModel>("input");
           return await ctx.Service<IRadialReviewRepository>().MeetingRemoveAttendee(args.MeetingId, args.UserId);
         });

      descriptor
        .Field("EditMeetingAttendeeIsPresent")
        .Argument("input", a => a.Type<NonNullType<MeetingAttendeeIsPresentType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) =>
        {
          var args = ctx.ArgumentValue<MeetingAttendeeIsPresentModel>("input");
          return await ctx.Service<IDataContext>().SetMeetingAttendeeIsPresent(args);
        });
    }
  }
}