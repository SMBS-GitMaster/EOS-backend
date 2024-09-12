namespace RadialReview.GraphQL.Types
{
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.GraphQL.Types;
  using RadialReview.Repositories;
  using System;
  using System.Linq;

  public class MeetingAttendeeChangeType : MeetingAttendeeQueryType
  {
    public MeetingAttendeeChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingAttendeeQueryModel> descriptor)
    {
      base.Configure(descriptor);
      
      descriptor.Name("MeetingAttendeeModelChange");
    }
  }

  public class MeetingAttendeeQueryType : ObjectType<MeetingAttendeeQueryModel>
  {
    protected readonly bool isSubscription; 

    public MeetingAttendeeQueryType()
      : this(false)
    {
    }

    protected MeetingAttendeeQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingAttendeeQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "meetingAttendee");

      descriptor
          .Field(t => t.Id)
          .Type<LongType>()
          .IsProjected(true);

      descriptor
          .Field(t => t.MeetingId)
          .Type<LongType>()
          .IsProjected(true);

      descriptor 
          .Field("permissions")
          .Type<ObjectType<MeetingPermissionsModel>>()
          .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetPermissionsForCallerOnMeetingAsync(ctx.Parent<MeetingAttendeeQueryModel>().MeetingId))
          .UseProjection() 
          ;

      descriptor 
          .Field("permissionz")
          .Type<ObjectType<MeetingPermissionsModel>>()
          .Resolve(async ctx => await ctx.BatchDataLoader<(long recurrenceId, long uomId), MeetingPermissionsModel>(async (keys, cancellationToken) => {
            using var session = Utilities.HibernateSession.GetCurrentSession();

            var repository = ctx.Service<IRadialReviewRepository>();

            var result = repository.GetPermissionsForAttendeesOnMeetingAsync(keys, session, cancellationToken);            
            var lookup = result.ToDictionary(x => (x.recurrenceId, x.uomId), x => x.perm);
            return lookup;
          }, "meetingattendee_permissions").LoadAsync((ctx.Parent<MeetingAttendeeQueryModel>().MeetingId, ctx.Parent<MeetingAttendeeQueryModel>().Id)))
          ;
    }
  }
}