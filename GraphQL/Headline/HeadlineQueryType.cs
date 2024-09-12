namespace RadialReview.GraphQL.Types
{
  using System.Linq;
  using HotChocolate.Types;
  using RadialReview.GraphQL.Models;
  using HotChocolate.Types.Pagination;
  using RadialReview.Repositories;

  public class HeadlineChangeType : HeadlineQueryType
  {
    public HeadlineChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<HeadlineQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("HeadlineModelChange");
    }
  }
  
  public class HeadlineQueryType : ObjectType<HeadlineQueryModel>
  {
    protected readonly bool isSubscription;

    public HeadlineQueryType() : this(false)
    {
    }

    protected HeadlineQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<HeadlineQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field(t => t.RecurrenceId)
        .IsProjected();

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "headline")
          ;

      descriptor
          .Field(t => t.Id)
          .Type<LongType>()
          ;

      if (isSubscription)
      {
        descriptor
          .Field("comments")
          .Type<ListType<CommentChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetCommentsAsync(RadialReview.Models.ParentType.PeopleHeadline, ctx.Parent<HeadlineQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;

        descriptor
          .Field("meeting")
          .Type<MeetingChangeType>()
          .Resolve(async (ctx, cancelationToken) =>
          {
            return await ctx.BatchDataLoader<long, MeetingQueryModel>(
                async (keys, ct) =>
                {
                  var result = await ctx.Service<IDataContext>().GetMeetingsByIds(keys.ToList(), LoadMeetingModel.False(), ct);
                  return result.ToDictionary(x => x.Id);
                }
              )
            .LoadAsync(ctx.Parent<HeadlineQueryModel>().RecurrenceId, cancelationToken);
          })
          ;
      }        
      else
      {
        descriptor
          .Field("comments")
          .Type<ListType<CommentQueryType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetCommentsAsync(RadialReview.Models.ParentType.PeopleHeadline, ctx.Parent<HeadlineQueryModel>().Id, cancellationToken))
          .UsePaging<CommentQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;

        descriptor
          .Field("meeting")
          .Type<MeetingQueryType>()
          .Resolve(async (ctx, cancelationToken) =>
          {
            return await ctx.BatchDataLoader<long, MeetingQueryModel>(
                async (keys, ct) =>
                {
                  var result = await ctx.Service<IDataContext>().GetMeetingsByIds(keys.ToList(), LoadMeetingModel.False(), ct);
                  return result.ToDictionary(x => x.Id);
                }
              )
            .LoadAsync(ctx.Parent<HeadlineQueryModel>().RecurrenceId, cancelationToken);   
          })
          ;
      }
    }
  }
}