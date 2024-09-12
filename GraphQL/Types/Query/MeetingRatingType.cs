using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.GraphQL.Types {

  public class MeetingRatingChangeType : MeetingRatingType
  {
    public MeetingRatingChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingRatingModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MeetingRatingModelChange");
    }
  }  
  public class MeetingRatingType : ObjectType<MeetingRatingModel> {
    protected bool isSubscription; 

    public MeetingRatingType()
      : this(false)
    { 
    }

    protected MeetingRatingType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingRatingModel> descriptor) {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "meetingRating");

      descriptor
          .Field(t => t.Id)
          .Type<LongType>();
    }
  }
}