using HotChocolate.Types;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;

namespace RadialReview.GraphQL.Types {

  public class MeetingNoteChangeType : MeetingNoteQueryType
  {
    public MeetingNoteChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingNoteQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MeetingNoteModelChange");
    }
  }

  public class MeetingNoteQueryType : ObjectType<MeetingNoteQueryModel> {
    protected bool isSubscription;

    public MeetingNoteQueryType()
      : this(false)
    {
    }

    protected MeetingNoteQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingNoteQueryModel> descriptor) {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "meetingNote");

      descriptor
          .Field(t => t.Id)
          .Type<LongType>();

      descriptor
          .Field(t => t.OwnerId)
          .Type<LongType>()
          .UseProjection()
          .IsProjected(true);


      descriptor
        .Field("owner")
        .Type<UserQueryType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetUserByIdAsync(ctx.Parent<MeetingNoteQueryModel>().OwnerId, cancellationToken))
        .UseProjection()
        .UseFiltering()
        .UseSorting();



    }
  }
}