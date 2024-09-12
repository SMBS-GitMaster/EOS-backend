namespace RadialReview.GraphQL.Types {
  using System.Collections.Generic;
  using System.Linq;
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;

  public class IssueChangeType : IssueQueryType
  {
    public IssueChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<IssueQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("IssueModelChange");
    }
  }

  public class SingleIssueType: IssueQueryType
  {
    public SingleIssueType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<IssueQueryModel> descriptor)
    {
      base.Configure(descriptor);
      descriptor.Name("SingleIssueModel");

      descriptor
      .Field("addToDepartmentPlan")
      .Type<BooleanType>()
      .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetIssueAddToDepartmentPlanAsync(ctx.Parent<IssueQueryModel>().Id, cancellationToken));
    }
  }

  public class IssueQueryType : ObjectType<IssueQueryModel> {
    protected readonly bool isSubscription;

    public IssueQueryType()
      : this(false)
    {
    }
    
    protected IssueQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<IssueQueryModel> descriptor) {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "issue");

      descriptor
        .Field(t => t.Id)
        .Type<LongType>()
        .IsProjected(true)
        ;

      descriptor
        .Field(t => t.SentFromIssueId)
        .Type<LongType>()
        .IsProjected(true)
        ;

      descriptor
        .Field(t => t.SentToIssueId)
        .Type<LongType>()
        .IsProjected(true)
        ;

      descriptor
       .Field(t => t.RecurrenceId)
       .Type<LongType>()
       .IsProjected(true)
       ;

      descriptor
        .Field(t => t.Context)
        .Type<IssueContextType>();

      descriptor
        .Field("meeting")
        .Type<MeetingQueryType>()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMeetingAsync(ctx.Parent<IssueQueryModel>().RecurrenceId, LoadMeetingModel.False(), cancellationToken));

      if (isSubscription)
      {
        descriptor
          .Field("issueHistoryEntries")
          .Type<ListType<IssueHistoryEntryChangeType>>()
          .Resolve(ctx => { return new List<IssueHistoryEntryQueryModel>(); })
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;

        descriptor
          .Field("comments")
          .Type<ListType<CommentChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetCommentsAsync(RadialReview.Models.ParentType.Issue, ctx.Parent<IssueQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;

        descriptor
          .Field(t => t.SentToIssue)
          .Type<IssueChangeType>()
          ;

        descriptor
          .Field("sentFromIssue")
          .Type<IssueChangeType>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetIssueByIdAsync(ctx.Parent<IssueQueryModel>().SentFromIssueId, cancellationToken))
          .UseProjection()
          ;

      }
      else
      {
        descriptor
          .Field("issueHistoryEntries")
          .Type<ListType<IssueHistoryEntryType>>()
          .Resolve(ctx => { return new List<IssueHistoryEntryQueryModel>(); })
          .UsePaging<IssueHistoryEntryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;

        descriptor
          .Field("comments")
          .Type<ListType<CommentQueryType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetCommentsAsync(RadialReview.Models.ParentType.Issue, ctx.Parent<IssueQueryModel>().Id, cancellationToken))
          .UsePaging<CommentQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;

        descriptor
          .Field(t => t.SentToIssue)
          .Type<IssueQueryType>()
          ;

        descriptor
          .Field("sentFromIssue")
          .Type<IssueQueryType>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetIssueByIdAsync(ctx.Parent<IssueQueryModel>().SentFromIssueId, cancellationToken))
          .UseProjection()
          ;
      }
    }
  }
}