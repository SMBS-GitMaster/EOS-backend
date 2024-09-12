namespace RadialReview.GraphQL.Types {
  using System.Linq;
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;

  public class IssueSentToChangeType : IssueSentToType
  {
    public IssueSentToChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<IssueSentToMeetingDTO> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("IssueSentToModelChange");
    }
  }

  public class IssueSentToType : ObjectType<IssueSentToMeetingDTO> {
    protected readonly bool isSubscription;

    public IssueSentToType()
      : this(false)
    {
    }

    protected IssueSentToType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<IssueSentToMeetingDTO> descriptor) {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "issueSentTo");
          ;

      descriptor
          .Field(t => t.Id)
          .Type<LongType>()
          .IsProjected(true);

      descriptor
          .Field(t => t.MeetingId)
          .Type<LongType>()
          .IsProjected(true);

      if (isSubscription)
      {
        descriptor
          .Field("issueHistoryEntries")
          .Type<ListType<IssueHistoryEntryChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, IssueHistoryEntryQueryModel>(async (meetingIds, cancellationToken) => {
            var result = await ctx.Service<IDataContext>().GetIssueHistoryEntriesForIssuesAsync(meetingIds, cancellationToken);
            return result.ToLookup(x => x.IssueId);
          }, "issueSentTo_issueHistoryEntries").LoadAsync(ctx.Parent<IssueSentToMeetingDTO>().MeetingId))
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;
      }
      else 
      {
        descriptor
          .Field("issueHistoryEntries")
          .Type<ListType<IssueHistoryEntryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, IssueHistoryEntryQueryModel>(async (meetingIds, cancellationToken) => {
            var result = await ctx.Service<IDataContext>().GetIssueHistoryEntriesForIssuesAsync(meetingIds, cancellationToken);
            return result.ToLookup(x => x.IssueId);
          }, "issueSentTo_issueHistoryEntries").LoadAsync(ctx.Parent<IssueSentToMeetingDTO>().MeetingId))
          .UsePaging<IssueHistoryEntryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;
      }
    }
  }
}