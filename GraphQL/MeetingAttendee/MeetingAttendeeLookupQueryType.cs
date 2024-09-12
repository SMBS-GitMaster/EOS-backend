namespace RadialReview.GraphQL.Types
{
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.GraphQL.Types;
  using RadialReview.Repositories;
  using System;
  using System.Linq;

  public class MeetingAttendeeLookupChangeType : MeetingAttendeeLookupQueryType
  {
    public MeetingAttendeeLookupChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingAttendeeQueryModelLookup> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MeetingAttendeeLookupModelChange");
    }
  }

  public class MeetingAttendeeLookupQueryType : ObjectType<MeetingAttendeeQueryModelLookup>
  {
    protected readonly bool isSubscription;

    public MeetingAttendeeLookupQueryType()
      : this(false)
    {
    }

    protected MeetingAttendeeLookupQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingAttendeeQueryModelLookup> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "meetingAttendeeLookup");

      descriptor
          .Field(t => t.MeetingId)
          .Type<LongType>()
          .IsProjected(true);

    }
  }
}