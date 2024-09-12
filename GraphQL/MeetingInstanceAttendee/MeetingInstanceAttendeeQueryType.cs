namespace RadialReview.GraphQL.Types
{
    using HotChocolate.Types;
    using RadialReview.GraphQL.Models;

    public class MeetingInstanceAttendeeChangeType : MeetingInstanceAttendeeQueryType
    {
        public MeetingInstanceAttendeeChangeType() : base(true)
        {
        }

        protected override void Configure(IObjectTypeDescriptor<MeetingInstanceAttendeeQueryModel> descriptor)
        {
        base.Configure(descriptor);

        descriptor.Name("MeetingInstanceAttendeeChange");
        }
    } 

    public class MeetingInstanceAttendeeQueryType : ObjectType<MeetingInstanceAttendeeQueryModel>
    {
        protected readonly bool isSubscription;

        public MeetingInstanceAttendeeQueryType() 
            : this(false)
        {
        }

        protected MeetingInstanceAttendeeQueryType(bool isSubscription)
        {
            this.isSubscription = isSubscription;
        }

        protected override void Configure(IObjectTypeDescriptor<MeetingInstanceAttendeeQueryModel> descriptor)
        {
            base.Configure(descriptor);

            descriptor
                .Field("type")
                .Type<StringType>()
                .Resolve(ctx => "meetingInstanceAttendee")
                ;

            descriptor
                .Field(t => t.Id)
                .Type<LongType>();
        }
    }
}