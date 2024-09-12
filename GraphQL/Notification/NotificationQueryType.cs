namespace RadialReview.GraphQL.Types {
  using HotChocolate.Types;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;

  public class NotificationChangeType : NotificationQueryType
  {
    public NotificationChangeType() 
      : base(isSubscriptiion: true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<NotificationQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("NotificationModelChange");
    }
  }

  public class NotificationQueryType : ObjectType<NotificationQueryModel> {
    protected readonly bool isSubscriptiion;

    public NotificationQueryType()
      : this(isSubscriptiion: false)
    {
    }

    protected NotificationQueryType(bool isSubscriptiion)
    {
      this.isSubscriptiion = isSubscriptiion;
    }

    protected override void Configure(IObjectTypeDescriptor<NotificationQueryModel> descriptor) {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "notification")
        ;

      descriptor
          .Field(t => t.Id)
          .Type<LongType>();

      descriptor
          .Field("mentioner")
          .Type<UserQueryType>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetUserByIdAsync(ctx.Parent<NotificationQueryModel>().UserId, cancellationToken))
          .UseProjection()
          ;

      // NOTE: This descriptor is added to satisfy dev-static GraphQL subscription.
      descriptor 
          .Field("businessPlanId")
          .Type<LongType>()
          .Resolve((ctx, cancellationToken) => 0) // TODO: Determine if this needs to be added to NotificcationModel
          ;

      descriptor
          .Field("isCoreProcessEnabled")
          .Type<LongType>()
          .Resolve((ctx, cancellationToken) => 0) // TODO: Determine if this needs to be added to NotificcationModel
          ;

      descriptor
        .Field(t => t.MeetingId)
        .Type<LongType>()
        .IsProjected(true);

      descriptor
          .Field("meeting")
          .Type<MeetingQueryType>()
          .Resolve(
              (ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingAsync(ctx.Parent<NotificationQueryModel>().MeetingId, cancellationToken)
          );

      descriptor
        .Field(t => t.MentionerId)
        .Type<LongType>()
        .IsProjected(true);

      descriptor
          .Field("mentioner")
          .Type<UserQueryType>()
          .Resolve(
              (ctx, cancellationToken) => ctx.Service<IDataContext>().GetUserByIdAsync(ctx.Parent<NotificationQueryModel>().MentionerId, cancellationToken)
          );

      descriptor
        .Field(t => t.TodoId)
        .Type<LongType>()
        .IsProjected(true);

      descriptor
          .Field("todo")
          .Type<TodoQueryType>()
          .Resolve(
              (ctx, cancellationToken) => ctx.Service<IDataContext>().GetTodoByIdAsync(ctx.Parent<NotificationQueryModel>().TodoId, cancellationToken)
          );


    }
  }
}