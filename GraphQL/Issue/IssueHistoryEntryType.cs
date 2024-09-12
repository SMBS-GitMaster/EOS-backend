namespace RadialReview.GraphQL.Types {
  using HotChocolate.Types;
  using RadialReview.GraphQL.Models;
  using RadialReview.GraphQL.Types;
  using RadialReview.Repositories;

  public class IssueHistoryEntryChangeType : IssueHistoryEntryType
  {
    public IssueHistoryEntryChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<IssueHistoryEntryQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("IssueHistoryEntryChange");
    }
  }

  public class IssueHistoryEntryType : ObjectType<IssueHistoryEntryQueryModel> {
    protected readonly bool isSubscription;
     
    public IssueHistoryEntryType()
      : this(false)
    {
    }

    protected IssueHistoryEntryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;    
    }

    protected override void Configure(IObjectTypeDescriptor<IssueHistoryEntryQueryModel> descriptor) {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "issueHistoryEntry");

      descriptor
          .Field(t => t.Id)
          .Type<LongType>();

      descriptor
          .Field(t => t.IssueId)
          .Type<LongType>()
          .IsProjected(true);

      descriptor
          .Field(t => t.MeetingId)
          .Type<LongType>()
          .IsProjected(true);

      descriptor
          .Field("meeting")
          .Type<MeetingQueryType>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMeetingAsync(ctx.Parent<IssueHistoryEntryQueryModel>().MeetingId, LoadMeetingModel.False(), cancellationToken));
      
    }
  }
}