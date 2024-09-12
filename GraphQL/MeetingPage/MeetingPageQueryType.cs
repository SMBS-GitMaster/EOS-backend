using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.GraphQL.Types {

  public class MeetingPageChangeType : MeetingPageQueryType
  {
    public MeetingPageChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingPageQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MeetingPageModelChange");
    }
  }

  public class MeetingPageQueryType : ObjectType<MeetingPageQueryModel> {
    protected bool isSubscription;

    public MeetingPageQueryType() 
      : this(false)
    {
      
    }

    protected MeetingPageQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingPageQueryModel> descriptor) {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "meetingPage");

      descriptor
          .Field(t => t.Id)
          .Type<LongType>();

      descriptor
        .Field(t => t.MeetingId)
        .Ignore();
    }
  }
}