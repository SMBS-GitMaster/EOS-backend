using System.Linq;
using HotChocolate.Types.Pagination;
using HotChocolate.Types;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;

namespace RadialReview.GraphQL.Types
{
  public class TodoChangeType : TodoQueryType
  {
    public TodoChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<TodoQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("TodoModelChange");
    }
  } 
   
  public class TodoQueryType : ObjectType<TodoQueryModel>
  {
    protected readonly bool isSubscription;

    public TodoQueryType()
      : this(false)
    {
    }

    protected TodoQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<TodoQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "todo");

      descriptor
        .Field(t => t.Id)
        .Type<LongType>();

      descriptor
        .Field(t => t.ForRecurrenceId)
        .Type<LongType>()
        .IsProjected(true);

      descriptor
        .Field("meeting")
        .Type<MeetingQueryType>()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMeetingAsync(ctx.Parent<TodoQueryModel>().ForRecurrenceId, LoadMeetingModel.False(), cancellationToken))
        .UseProjection()
        ;

      if (isSubscription)
      {
        descriptor
          .Field("comments")
          .Type<ListType<CommentChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetCommentsAsync(RadialReview.Models.ParentType.Issue, ctx.Parent<IssueQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;
      }
      else
      {
        descriptor
          .Field("comments")
          .Type<ListType<CommentQueryType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetCommentsAsync(RadialReview.Models.ParentType.Issue, ctx.Parent<IssueQueryModel>().Id, cancellationToken))
          .UsePaging<CommentQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;
      }
    }
  }
}